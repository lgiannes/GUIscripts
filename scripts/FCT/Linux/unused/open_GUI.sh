#!/bin/bash

# this was meant to be an attempt to open and close the GUI from a bash script
# this piece of code is copied in all the scripts that use the GUI

source $FCT_RUN_FOLDER/setup.sh
GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI

if [ -z $(pidof mono) ]
then


( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1


else


( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1

fi