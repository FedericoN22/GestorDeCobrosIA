$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5165'
$numero = '5491100000000'
$numeroNoAutorizado = '5491111111111'
$script:fallos = 0
$script:ok = 0

function Invoke-Json($method, $url, $token, $body) {
    $headers = @{ Authorization = "Bearer $token" }
    $params = @{ Uri = "$base$url"; Method = $method; Headers = $headers; UseBasicParsing = $true }
    if ($null -ne $body) {
        $params.ContentType = 'application/json'
        $params.Body = ($body | ConvertTo-Json -Depth 10 -Compress)
    }
    $resp = Invoke-WebRequest @params
    $reader = New-Object System.IO.StreamReader($resp.RawContentStream, [System.Text.Encoding]::UTF8)
    return ($reader.ReadToEnd() | ConvertFrom-Json)
}

function Send-Bot($token, $num, $texto) {
    $resp = Invoke-Json 'POST' '/api/whatsapp/simular' $token @{ numero = $num; texto = $texto }
    return $resp.respuesta
}

function Assert-Contains($actual, $esperado, $etiqueta) {
    if ($actual -like "*$esperado*") {
        $script:ok++
        Write-Host "  OK - $etiqueta"
    }
    else {
        $script:fallos++
        Write-Host "  FALLO - $etiqueta"
        Write-Host "  esperado contenia: $esperado"
        Write-Host "  respuesta: $actual"
    }
}

$login = Invoke-WebRequest -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body '{"username":"admin","password":"admin123"}' -UseBasicParsing
$loginReader = New-Object System.IO.StreamReader($login.RawContentStream, [System.Text.Encoding]::UTF8)
$token = ($loginReader.ReadToEnd() | ConvertFrom-Json).token
Write-Host "== Login OK =="

$nombreProducto = "Gaseosa Cola $(Get-Date -Format 'yyyyMMddHHmmss')"
$prod = Invoke-Json 'POST' '/api/productos' $token @{ nombre = $nombreProducto; categoriaId = $null }
$productoId = $prod.productoId
$pres = Invoke-Json 'POST' "/api/productos/$productoId/presentaciones" $token @{ nombre = '2 Litros'; precioVentaCentavos = 420000; precioCostoCentavos = 280000; codigoBarras = $null }
Invoke-Json 'POST' '/api/stock/entrada' $token @{ presentacionId = $pres.id; cantidad = 50; precioCostoCentavos = 280000 } | Out-Null
Write-Host "== Producto '$nombreProducto' 2 Litros con stock 50 = OK =="

Write-Host ''
Write-Host '--- 1. Saludo ---'
$r = Send-Bot $token $numero 'hola'
Assert-Contains $r 'Puedo ayudarte' 'Saludo devuelve la ayuda del bot'

Write-Host '--- 2. Consultar stock ---'
$r = Send-Bot $token $numero "cuanto stock hay de $nombreProducto 2 litros"
Assert-Contains $r 'Stock de' 'Respuesta de stock'
Assert-Contains $r '50 unidad' 'Cantidad de stock'

Write-Host '--- 3. Consultar precio ---'
$r = Send-Bot $token $numero "cuanto sale $nombreProducto 2 litros"
Assert-Contains $r 'Precio de' 'Respuesta de precio'
Assert-Contains $r '$4.200,00' 'Precio formateado en pesos'

Write-Host '--- 4. Agregar stock con cantidad y costo ---'
$r = Send-Bot $token $numero "agregar $nombreProducto 2 litros cantidad 10 costo 2500"
Assert-Contains $r 'se agregaron 10 unidades' 'Respuesta de alta de stock'

Write-Host '--- 5. Falta cantidad ---'
$r = Send-Bot $token $numero "agregar $nombreProducto 2 litros"
Assert-Contains $r 'Me falta informaci' 'Pide los campos faltantes'
Assert-Contains $r 'cantidad' 'Menciona cantidad como faltante'

Write-Host '--- 6. Multi comando ---'
$r = Send-Bot $token $numero "agregar $nombreProducto 2 litros cantidad 5 y eliminar $nombreProducto"
Assert-Contains $r 'de una instrucci' 'Rechaza el multi comando'

Write-Host '--- 7. Cambiar precio pide confirmacion ---'
$r = Send-Bot $token $numero "cambiar precio de $nombreProducto 2 litros a 5000"
Assert-Contains $r 'Confirm' 'Pide confirmacion'
Assert-Contains $r '$5.000,00' 'Muestra el precio nuevo'

Write-Host '--- 8. Confirmar cambio de precio ---'
$r = Send-Bot $token $numero 'SI'
Assert-Contains $r 'Precio actualizado' 'Confirma y ejecuta el cambio'
Assert-Contains $r '$5.000,00' 'Precio nuevo aplicado'

Write-Host '--- 9. Eliminar pide confirmacion ---'
$r = Send-Bot $token $numero "eliminar $nombreProducto 2 litros"
Assert-Contains $r 'eliminar' 'Pide confirmacion de baja'
Assert-Contains $r 'Stock actual' 'Muestra el stock'

Write-Host '--- 10. Confirmar eliminacion ---'
$r = Send-Bot $token $numero 'SI'
Assert-Contains $r 'elimin' 'Ejecuta la baja'

Write-Host '--- 11. Numero fuera de whitelist ---'
$r = Send-Bot $token $numeroNoAutorizado 'hola'
Assert-Contains $r 'autorizado' 'Rechaza numeros no autorizados'

Write-Host ''
Write-Host '=== Admin: whitelist (RF-040) ==='

Write-Host '--- 12. Listar whitelist ---'
$wl = Invoke-Json 'GET' '/api/whatsapp/whitelist' $token $null
$activos = @($wl | Where-Object { $_.activo })
Assert-Contains (($activos | ForEach-Object { $_.whatsappNumero }) -join ',') $numero 'La whitelist incluye el numero sembrado'

Write-Host '--- 13. Agregar numero a la whitelist ---'
$numeroAdmin = "549$(Get-Date -Format 'HHmmss')"
$agregado = Invoke-Json 'POST' '/api/whatsapp/whitelist' $token @{ whatsappNumero = $numeroAdmin }
Assert-Contains $agregado.whatsappNumero $numeroAdmin 'Agrega el numero'
Assert-Contains "$($agregado.activo)" 'True' 'Queda activo'
$whitelistId = $agregado.id

Write-Host '--- 14. Numero duplicado ---'
$duplicado = $false
try {
    Invoke-Json 'POST' '/api/whatsapp/whitelist' $token @{ whatsappNumero = $numeroAdmin } | Out-Null
}
catch {
    $duplicado = $true
}
if ($duplicado) {
    $script:ok++
    Write-Host '  OK - Rechaza el numero duplicado'
}
else {
    $script:fallos++
    Write-Host '  FALLO - Deberia rechazar el numero duplicado'
}

Write-Host '--- 15. Quitar numero de la whitelist ---'
$quitado = Invoke-Json 'DELETE' "/api/whatsapp/whitelist/$whitelistId" $token $null
Assert-Contains "$($quitado.activo)" 'False' 'Queda desactivado'

Write-Host ''
Write-Host '=== Admin: configuracion del bot (RF-039) ==='

Write-Host '--- 16. Obtener configuracion del bot ---'
$cfg = Invoke-Json 'GET' '/api/whatsapp/config/bot' $token $null
Assert-Contains $cfg.nombre 'asistente' 'Nombre por defecto'
Assert-Contains "$($cfg.tiempoConfirmacionMinutos)" '2' 'Timeout por defecto'

Write-Host '--- 17. Guardar configuracion del bot ---'
$guardada = Invoke-Json 'PUT' '/api/whatsapp/config/bot' $token @{ nombre = 'Bot Demo'; bienvenida = 'Bienvenido al kiosco!'; tiempoConfirmacionMinutos = 5; limiteMensajesPorMinuto = 20 }
Assert-Contains $guardada.nombre 'Bot Demo' 'Guarda el nombre'
Assert-Contains "$($guardada.tiempoConfirmacionMinutos)" '5' 'Guarda el timeout'

Write-Host '--- 18. La configuracion persiste ---'
$cfg2 = Invoke-Json 'GET' '/api/whatsapp/config/bot' $token $null
Assert-Contains $cfg2.nombre 'Bot Demo' 'Persiste el nombre'
Assert-Contains "$($cfg2.limiteMensajesPorMinuto)" '20' 'Persiste el limite'

Write-Host '--- 19. Restaurar configuracion por defecto ---'
Invoke-Json 'PUT' '/api/whatsapp/config/bot' $token @{ nombre = 'asistente'; bienvenida = ''; tiempoConfirmacionMinutos = 2; limiteMensajesPorMinuto = 10 } | Out-Null
$script:ok++
Write-Host '  OK - Configuracion restaurada'

Write-Host ''
Write-Host "RESULTADO: $($script:ok) OK, $($script:fallos) fallos"
if ($script:fallos -gt 0) {
    exit 1
}
