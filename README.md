# WindowsGSM.RCT2
⚠Work-In-Progress⚠ 🧩 WindowsGSM plugin for supporting Roller Coaster Tycoon 2

## Requirements
[WindowsGSM](https://github.com/WindowsGSM/WindowsGSM) >= 1.21.0

You must also own the game Roller Coaster Tycoon 2!

You will need to supply it with your own .sv6 files (World or Scenario files) OR you can choose to point it to one of the ones I hosted with this plugin (In the new Scenario folder, point it directly to the URL in your server params)

## Installation
1. Move **RCT2.cs** folder to **plugins** folder
1. Click **[RELOAD PLUGINS]** button or restart WindowsGSM
1. Make sure your server parameters are similar to this || host "C:\Users\Bansh\Documents\OpenRCT2\save\autosave\autosave_2019-11-30_23-28-38.sv6" --headless

## Known issues
The process ID doesn't get handed off to windowsGSM so the server stays in a perpetual state of "Started", also stopping the server doesn't work gracefully

## Additional Command Line options

    -h, --help                    show this help message and exit
    -v, --version                 show version information and exit
    -n, --no-install              do not install scenario if passed
    -a, --all                     show help for all commands
    --about                       show information about OpenRCT2
    --verbose                     log verbose messages
    --headless                    run OpenRCT2 headless, implies --silent-breakpad
    --port=<int>                  port to use for hosting or joining a server
    --address=<str>               address to listen on when hosting a server
    --password=<str>              password needed to join the server
    --user-data-path=<str>        path to the user data directory (containing config.ini)
    --openrct2-data-path=<str>    path to the OpenRCT2 data directory (containing languages)
    --rct1-data-path=<str>        path to the RollerCoaster Tycoon 1 data directory (containing data/csg1.dat)
    --rct2-data-path=<str>        path to the RollerCoaster Tycoon 2 data directory (containing data/g1.dat)
    --silent-breakpad             make breakpad crash reporting silent


### License
This project is licensed under the MIT License - see the [LICENSE.md](https://github.com/BattlefieldDuck/WindowsGSM.ARMA3/blob/master/LICENSE) file for details

