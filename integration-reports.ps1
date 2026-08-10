$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5165'

function Invoke-Json($method, $url, $token, $body) {
    $headers = @{ Authorization = "Bearer $token" }
    $params = @{ Uri = "$base$url"; Method = $method; Headers = $headers; UseBasicParsing = $true }
    if ($null -ne $body) {
        $params.ContentType = 'application/json'
        $params.Body = ($body | ConvertTo-Json -Depth 10 -Compress)
    }
    $resp = Invoke-WebRequest @params
    return $resp.Content | ConvertFrom-Json
}

$login = Invoke-WebRequest -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body '{"username":"admin","password":"admin123"}' -UseBasicParsing
$token = ($login.Content | ConvertFrom-Json).token
Write-Host "== Login OK =="

$nombreProducto = "Gaseosa Cola $(Get-Date -Format 'yyyyMMddHHmmss')"
$prod = Invoke-Json 'POST' '/api/productos' $token @{ nombre = $nombreProducto; categoriaId = $null }
$productoId = $prod.productoId
$pres = Invoke-Json 'POST' "/api/productos/$productoId/presentaciones" $token @{ nombre = '600 ml'; precioVentaCentavos = 1500; precioCostoCentavos = 900; codigoBarras = $null }
$presId = $pres.id
Write-Host "== Producto creado: presentacion $presId =="

Invoke-Json 'POST' '/api/stock/entrada' $token @{ presentacionId = $presId; cantidad = 100; precioCostoCentavos = 900 } | Out-Null
Write-Host '== Stock +100 = OK =='

Invoke-Json 'POST' '/api/cajas/abrir' $token @{ montoInicialCentavos = 5000 } | Out-Null
Write-Host '== Caja abierta = OK =='

$venta = Invoke-Json 'POST' '/api/ventas' $token @{ lineas = @(@{ presentacionId = $presId; cantidad = 2 }); pagos = @(@{ medio = 1; montoCentavos = 3000 }) }
Write-Host "== Venta registrada: total $($venta.totalCentavos), numero $($venta.numero) = OK =="

Invoke-Json 'POST' '/api/cajas/cerrar' $token @{ montoEsperadoCentavos = 8000; montoDeclaradoCentavos = 7800 } | Out-Null
Write-Host '== Caja cerrada = OK =='

$hoy = Get-Date -Format 'yyyy-MM-dd'
$rango = "?desde=$hoy&hasta=$hoy"

Write-Host ''
Write-Host '--- R1 ventas ---'
Invoke-Json 'GET' "/api/reportes/ventas$rango" $token | ConvertTo-Json -Depth 10
Write-Host '--- R5 cierres ---'
Invoke-Json 'GET' "/api/reportes/cierres$rango" $token | ConvertTo-Json -Depth 10
Write-Host '--- R4 movimientos ---'
Invoke-Json 'GET' "/api/reportes/movimientos$rango" $token | ConvertTo-Json -Depth 10
Write-Host '--- R2/R3 ganancias ---'
Invoke-Json 'GET' "/api/reportes/ganancias$rango" $token | ConvertTo-Json -Depth 10
Write-Host '--- R6 ranking ---'
Invoke-Json 'GET' "/api/reportes/ranking$rango" $token | ConvertTo-Json -Depth 10
Write-Host '--- R7 auditoria ---'
Invoke-Json 'GET' "/api/reportes/auditoria$rango" $token | ConvertTo-Json -Depth 10
Write-Host '--- CSV ventas (primeras lineas) ---'
$csv = Invoke-WebRequest -Uri "$base/api/reportes/ventas.csv$rango" -Headers @{ Authorization = "Bearer $token" } -UseBasicParsing
$csv.Content -split "`n" | Select-Object -First 4
Write-Host '--- CSV auditoria (primeras lineas) ---'
$csvA = Invoke-WebRequest -Uri "$base/api/reportes/auditoria.csv$rango" -Headers @{ Authorization = "Bearer $token" } -UseBasicParsing
$csvA.Content -split "`n" | Select-Object -First 4
