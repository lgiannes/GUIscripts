#!/bin/bash

SN=$1
channel=$2

source $FCT_RUN_FOLDER/setup.sh

exe_path=$ANALYSIS_FOLDER"/bin/";


# Check that the pulse generator is connected. Otherwise, abort script
if bash $FCT_UTILS"/check_fg.sh" | grep -q '/dev/ttyACM0'; 
then
  echo "Pulse Gen is connected to: /dev/ttyACM0" 
else
  echo "ERROR: Pulse Gen is NOT connected.  " 
  exit
fi

sudo chmod 777 $GENERALDATADIR

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI



# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/one_channel_test/Script_one_ch.cs\",$SN,$channel)"
# Close all GUIs to avoid double serial com
if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi
# Open GUI and wait 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; sleep 13; } | telnet $ip_address $port 

# Close the GUI
if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi
file_name=$GENERALDATADIR"FEBs/SN_"$SN"/Single_Channels_Tests/one_ch_test_SN"$SN"_ch"$channel".daq"

$exe_path/OneChannelTest -f $file_name
echo "done"