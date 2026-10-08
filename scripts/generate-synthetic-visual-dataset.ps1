$ErrorActionPreference = "Stop"
$assetDirectory = Join-Path $PSScriptRoot "..\tests\EmergencyResponse.CopilotEvaluation\Datasets\VisualAnalysis\Synthetic"
New-Item -ItemType Directory -Force -Path $assetDirectory | Out-Null

$scenes = @(
    @{ Name = "flood"; Background = "0xA8D8F0"; Filters = "drawbox=x=0:y=180:w=640:h=90:color=0x55585C:t=fill,drawbox=x=0:y=260:w=640:h=100:color=0x267CA3:t=fill,drawbox=x=210:y=205:w=180:h=55:color=0xD9D9D9:t=fill,drawbox=x=235:y=218:w=40:h=18:color=0x222222:t=fill,drawbox=x=330:y=218:w=40:h=18:color=0x222222:t=fill" },
    @{ Name = "fire"; Background = "0xB7C9D6"; Filters = "drawbox=x=110:y=100:w=420:h=250:color=0x64686C:t=fill,drawbox=x=215:y=175:w=75:h=120:color=0x1C242B:t=fill,drawbox=x=320:y=175:w=75:h=120:color=0x1C242B:t=fill,drawbox=x=295:y=30:w=60:h=100:color=0xF06423:t=fill,drawbox=x=270:y=55:w=35:h=70:color=0xFFC247:t=fill" },
    @{ Name = "smoke"; Background = "0xD1DEE5"; Filters = "drawbox=x=150:y=150:w=350:h=200:color=0x777B80:t=fill,drawbox=x=220:y=50:w=170:h=80:color=0x92979D:t=fill,drawbox=x=300:y=15:w=120:h=70:color=0xA5A9AE:t=fill" },
    @{ Name = "structural-damage"; Background = "0xBBCBD5"; Filters = "drawbox=x=120:y=80:w=390:h=280:color=0x77716A:t=fill,drawbox=x=120:y=270:w=160:h=90:color=0xB6A58F:t=fill,drawbox=x=290:y=80:w=28:h=210:color=0x332F2B:t=fill,drawbox=x=325:y=205:w=185:h=25:color=0x3B3732:t=fill" },
    @{ Name = "road-blockage"; Background = "0xB5D1DE"; Filters = "drawbox=x=0:y=190:w=640:h=170:color=0x55585C:t=fill,drawbox=x=180:y=185:w=135:h=100:color=0x846B58:t=fill,drawbox=x=300:y=215:w=105:h=90:color=0xA58A70:t=fill,drawbox=x=400:y=180:w=90:h=115:color=0x746150:t=fill" },
    @{ Name = "people-trapped"; Background = "0xA8D8F0"; Filters = "drawbox=x=0:y=205:w=640:h=155:color=0x287DA1:t=fill,drawbox=x=150:y=110:w=280:h=100:color=0x55585C:t=fill,drawbox=x=245:y=80:w=45:h=130:color=0x222222:t=fill,drawbox=x=345:y=80:w=45:h=130:color=0x222222:t=fill,drawbox=x=255:y=95:w=25:h=25:color=0xE0B998:t=fill,drawbox=x=355:y=95:w=25:h=25:color=0xE0B998:t=fill" },
    @{ Name = "vehicles"; Background = "0xB7D3E0"; Filters = "drawbox=x=0:y=220:w=640:h=140:color=0x55585C:t=fill,drawbox=x=90:y=185:w=170:h=65:color=0xD74E3C:t=fill,drawbox=x=390:y=185:w=170:h=65:color=0xE5B93E:t=fill,drawbox=x=125:y=230:w=38:h=22:color=0x202020:t=fill,drawbox=x=195:y=230:w=38:h=22:color=0x202020:t=fill,drawbox=x=425:y=230:w=38:h=22:color=0x202020:t=fill,drawbox=x=495:y=230:w=38:h=22:color=0x202020:t=fill" },
    @{ Name = "no-emergency"; Background = "0x9BCB8A"; Filters = "drawbox=x=0:y=265:w=640:h=95:color=0x59834F:t=fill,drawbox=x=90:y=170:w=160:h=130:color=0xD9D1B8:t=fill,drawbox=x=390:y=145:w=125:h=160:color=0xAAA995:t=fill,drawbox=x=0:y=305:w=640:h=55:color=0x777B72:t=fill" },
    @{ Name = "ambiguous"; Background = "0x77858A"; Filters = "drawbox=x=200:y=120:w=210:h=170:color=0x51575A:t=fill,drawbox=x=0:y=265:w=640:h=95:color=0x46494B:t=fill,drawbox=x=250:y=180:w=60:h=35:color=0x687074:t=fill" }
)

foreach ($scene in $scenes) {
    $output = Join-Path $assetDirectory "$($scene.Name).png"
    & ffmpeg -hide_banner -loglevel error -f lavfi -i "color=c=$($scene.Background):s=640x360:d=1" -frames:v 1 -vf $scene.Filters -y $output
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $output)) {
        throw "Failed to generate synthetic visual asset '$($scene.Name)'."
    }
    $jpegOutput = Join-Path $assetDirectory "$($scene.Name).jpg"
    & ffmpeg -hide_banner -loglevel error -i $output -frames:v 1 -q:v 2 -y $jpegOutput
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $jpegOutput)) {
        throw "Failed to generate synthetic JPEG frame '$($scene.Name)'."
    }
}
