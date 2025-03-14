pi case monitor
pi
**r**k*

On thhe pi (piMon):
cd /etc/xdg/lxsession/LXDE-pi 
sudo nano autostart

then edit the url address

Chromium startup:
@chromium-browser --kiosk --enable-features=OverlayScrollbar --disable-restore-session-state http://ruairipc/hardmon


to build production version of Angular app:

ng build --prod --base-href "./"


ADLX DLL build:

This DLL is built using the ADLXDLLBuilds solution which can be found under the ADLX-1.3 SDK folder.

After building, you need to copy the ADLXCSharpBind.DLL to the Lib folder in this project and the .cs files from the out folder to the ADLX folder in this project.

Check the ADLX-1.3\SDKDoc folder for the documentation on how to build and use the DLL.