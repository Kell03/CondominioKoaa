window.descargarArchivoDesdeBytes = (nombre, base64) => {
    const link = document.createElement('a');
    link.download = nombre;
    link.href = 'data:application/octet-stream;base64,' + base64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.esPantallaMovil = () => {
    return window.matchMedia("(max-width: 768px)").matches;
};