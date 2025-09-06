import { useState, useEffect } from 'react';

interface ARSupport {
    webglSupported: boolean;
    xrSupported: boolean;
    arSupported: boolean;
    loading: boolean;
    error: string | null;
}

const useARSupport = (): ARSupport => {
    const [support, setSupport] = useState<ARSupport>({
        webglSupported: false,
        xrSupported: false,
        arSupported: false,
        loading: true,
        error: null
    });

    useEffect(() => {
        const checkARSupport = async () => {
            try {
                const webglSupported = checkWebGLSupport();
                
                const xrSupported = 'xr' in navigator;
                
                let arSupported = false;
                if (xrSupported) {
                    try {
                        if (navigator.xr) {
                            arSupported = await navigator.xr.isSessionSupported('immersive-ar');
                        }
                    } catch (err) {
                        console.warn('AR session support check failed:', err);
                    }
                }
                
                setSupport({
                    webglSupported,
                    xrSupported,
                    arSupported: arSupported && webglSupported,
                    loading: false,
                    error: null
                });
            } catch (error) {
                setSupport({
                    webglSupported: false,
                    xrSupported: false,
                    arSupported: false,
                    loading: false,
                    error: error instanceof Error ? error.message : 'Failed to check AR support'
                });
            }
        };

        void checkARSupport();
    }, []);

    return support;
};

const checkWebGLSupport = (): boolean => {
    try {
        const canvas = document.createElement('canvas');
        const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
        return !!gl;
    } catch {
        return false;
    }
};

export default useARSupport;