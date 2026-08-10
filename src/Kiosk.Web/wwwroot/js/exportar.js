window.descargarCsv = async function (url, token, nombreArchivo) {
    try {
        const respuesta = await fetch(url, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!respuesta.ok) {
            let mensaje = 'Error al exportar (' + respuesta.status + ').';
            try {
                const error = await respuesta.json();
                if (error && error.message) mensaje = error.message;
            } catch (_) { }
            alert(mensaje);
            return;
        }
        const blob = await respuesta.blob();
        const urlObjeto = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = urlObjeto;
        enlace.download = nombreArchivo + '.csv';
        document.body.appendChild(enlace);
        enlace.click();
        document.body.removeChild(enlace);
        URL.revokeObjectURL(urlObjeto);
    } catch (error) {
        alert('Error al exportar el CSV: ' + error);
    }
};
