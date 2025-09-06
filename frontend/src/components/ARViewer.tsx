import React, { useState, useEffect, useRef } from 'react';
import * as THREE from 'three';
import { DRACOLoader } from 'three/addons/loaders/DRACOLoader.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

import useARSupport from '../hooks/useARSupport';

interface ARViewerProps {
    productId: number;
    productName: string;
    onClose: () => void;
}

const ARViewer: React.FC<ARViewerProps> = ({ productId, productName, onClose }) => {
    const [loading, setLoading] = useState(true);
    const [progress, setProgress] = useState(0);
    const [error, setError] = useState<string | null>(null);
    const [arSession, setArSession] = useState<XRSession | null>(null);
    const [fallbackImage, setFallbackImage] = useState<string | null>(null);
    
    const arContainerRef = useRef<HTMLDivElement>(null);
    const sceneRef = useRef<THREE.Scene | null>(null);
    const cameraRef = useRef<THREE.PerspectiveCamera | null>(null);
    const rendererRef = useRef<THREE.WebGLRenderer | null>(null);
    const modelRef = useRef<THREE.Object3D | null>(null);
    
    const arSupport = useARSupport();

    useEffect(() => {
        if (!arContainerRef.current) {
            return;
        }

        const shouldUseFallback = !arSupport.webglSupported || !arSupport.arSupported || arSupport.error;
        
        if (shouldUseFallback) {
            setFallbackImage(`${import.meta.env.VITE_BASE_URL}/api/product/${productId}/image/ar-fallback`);
            setLoading(false);
            return;
        }

        initARScene();
        
        return () => {
            cleanupARScene();
        };
    }, [arSupport.webglSupported, arSupport.arSupported, arSupport.error]);

    const initARScene = () => {
        try {
            const scene = new THREE.Scene();
            sceneRef.current = scene;
            
            const camera = new THREE.PerspectiveCamera(
                75,
                window.innerWidth / window.innerHeight,
                0.1,
                1000
            );
            cameraRef.current = camera;
            
            const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
            renderer.setSize(window.innerWidth, window.innerHeight);
            renderer.setPixelRatio(window.devicePixelRatio);
            arContainerRef.current?.appendChild(renderer.domElement);
            rendererRef.current = renderer;
            
            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
            scene.add(ambientLight);
            
            const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
            directionalLight.position.set(1, 1, 1);
            scene.add(directionalLight);
            
            loadModel();
            
            const animate = () => {
                requestAnimationFrame(animate);
                if (modelRef.current) {
                    modelRef.current.rotation.y += 0.01;
                }
                renderer.render(scene, camera);
            };
            animate();
            
            const handleResize = () => {
                if (cameraRef.current && rendererRef.current) {
                    cameraRef.current.aspect = window.innerWidth / window.innerHeight;
                    cameraRef.current.updateProjectionMatrix();
                    rendererRef.current.setSize(window.innerWidth, window.innerHeight);
                }
            };
            
            window.addEventListener('resize', handleResize);
            
            return () => {
                window.removeEventListener('resize', handleResize);
            };
        } catch (err) {
            setError('Failed to initialize AR scene');
            console.error('AR scene initialization error:', err);
            setLoading(false);
        }
    };

    const loadModel = () => {
        try {
            const dracoLoader = new DRACOLoader();
            dracoLoader.setDecoderPath('https://www.gstatic.com/draco/versioned/decoders/1.4.3/');
            
            const loader = new GLTFLoader();
            loader.setDRACOLoader(dracoLoader);
            
            const modelUrl = `${import.meta.env.VITE_BASE_URL}/api/product/${productId}/model/3d`;
            
            setLoading(true);
            setProgress(0);
            
            // Load the model with progress tracking
            loader.load(
                modelUrl,
                (gltf: { scene: THREE.Group }) => {
                    setProgress(100);
                    
                    if (sceneRef.current) {
                        if (modelRef.current) {
                            sceneRef.current.remove(modelRef.current);
                        }
                        
                        const model = gltf.scene;
                        model.scale.set(0.5, 0.5, 0.5);
                        model.position.set(0, 0, 0);
                        
                        sceneRef.current.add(model);
                        modelRef.current = model;
                    }
                    
                    setLoading(false);
                },
                (xhr: ProgressEvent) => {
                    const progress = (xhr.loaded / xhr.total) * 100;
                    setProgress(Math.min(95, Math.round(progress))); // Cap at 95% until fully loaded
                },
                (error: unknown) => {
                    console.error('Error loading 3D model:', error);
                    setError('Failed to load 3D model. Please try again.');
                    setLoading(false);
                    
                    createPlaceholderModel();
                }
            );
            
        } catch (err) {
            setError('Failed to load 3D model');
            console.error('Model loading error:', err);
            setLoading(false);
            createPlaceholderModel();
        }
    };

    const createPlaceholderModel = () => {
        if (sceneRef.current) {
            const geometry = new THREE.BoxGeometry(1, 1, 1);
            const material = new THREE.MeshStandardMaterial({
                color: 0x00ff00,
                metalness: 0.3,
                roughness: 0.4
            });
            const cube = new THREE.Mesh(geometry, material);
            sceneRef.current.add(cube);
            modelRef.current = cube;
        }
    };

    const cleanupARScene = () => {
        if (arSession) {
            void arSession.end();
        }
        
        if (rendererRef.current) {
            rendererRef.current.dispose();
        }
        if (arContainerRef.current && rendererRef.current?.domElement) {
            arContainerRef.current.removeChild(rendererRef.current.domElement);
        }
        
        if (sceneRef.current) {
            while(sceneRef.current.children.length > 0) {
                const child = sceneRef.current.children[0];
                if (child instanceof THREE.Mesh) {
                    const mesh = child as THREE.Mesh;
                    (mesh.geometry).dispose();
                    if (Array.isArray(mesh.material)) {
                        mesh.material.forEach(material => (material).dispose());
                    } else {
                        (mesh.material).dispose();
                    }
                }
                sceneRef.current.remove(child);
            }
        }
    };

    const startARSession = async () => {
        if (!navigator.xr) {
            setError('WebXR not supported');
            return;
        }

        try {
            const session = await navigator.xr.requestSession('immersive-ar', {
                requiredFeatures: ['hit-test']
            });
            setArSession(session);
            
            session.addEventListener('end', () => {
                setArSession(null);
            });
        } catch (err) {
            setError('Failed to start AR session. Your device may not support AR.');
            console.error('AR session error:', err);
        }
    };

    if (arSupport.loading) {
        return (
            <div className="ar-viewer">
                <div className="ar-loading">Checking AR support...</div>
            </div>
        );
    }

    if (arSupport.error) {
        return (
            <div className="ar-viewer">
                <div className="ar-error">
                    <p>Error: {arSupport.error}</p>
                    <button onClick={onClose}>Close</button>
                </div>
            </div>
        );
    }

    if (!arSupport.webglSupported) {
        return (
            <div className="ar-viewer">
                <div className="ar-error">
                    <p>WebGL is not supported in your browser. AR features are not available.</p>
                    <button onClick={onClose}>Close</button>
                </div>
            </div>
        );
    }

    if (!arSupport.arSupported && fallbackImage) {
        return (
            <div className="ar-viewer fallback-viewer">
                <div className="fallback-content">
                    <h3>AR Try-On - {productName}</h3>
                    <img
                        src={fallbackImage}
                        alt={`AR view of ${productName}`}
                        className="fallback-image"
                    />
                    <p>AR is not supported on your device. This is a static preview.</p>
                    <button onClick={onClose} className="close-button">Close</button>
                </div>
            </div>
        );
    }

    return (
        <div className="ar-viewer" ref={arContainerRef}>
            {loading && (
                <div className="ar-loading-overlay">
                    <div className="ar-loading-content">
                        <div className="loading-spinner"></div>
                        <p>Loading 3D model... {progress}%</p>
                    </div>
                </div>
            )}
            
            {error && (
                <div className="ar-error-overlay">
                    <div className="ar-error-content">
                        <p>Error: {error}</p>
                        <button onClick={onClose}>Close</button>
                    </div>
                </div>
            )}
            
            {!loading && !error && arSupport.arSupported && (
                <div className="ar-controls">
                    <button onClick={() => void startARSession()} className="ar-start-button">
                        Start AR Experience
                    </button>
                    <button onClick={onClose} className="ar-close-button">Close</button>
                </div>
            )}
        </div>
    );
};

export default ARViewer;