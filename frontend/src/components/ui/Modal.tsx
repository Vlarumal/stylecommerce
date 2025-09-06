import { Modal as AntModal, ModalProps as AntModalProps } from 'antd';
import React from 'react';

interface ModalProps extends Omit<AntModalProps, 'open' | 'onCancel'> {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  ariaLabel: string;
}

const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  ariaLabel,
  ...rest
}) => {
  return (
    <AntModal
      open={isOpen}
      onCancel={onClose}
      title={title}
      footer={null}
      aria-label={ariaLabel}
      {...rest}
    >
      {children}
    </AntModal>
  );
};

export default Modal;