#!/bin/bash

SN=$1
channel=$2

export DATADIR="/home/neutrino/FCT/data_local/"
exe_path="/home/neutrino/FCT/FunctionalTest/bin/";


# Check that the pulse generator is connected. Otherwise, abort script
if bash $FCT_RUN_FOLDER"/check_fg.sh" | grep -q '/dev/ttyACM0'; 
then
  echo "Pulse Gen is connected to: /dev/ttyACM0" 
else
  echo "ERROR: Pulse Gen is NOT connected.  " 
  exit
fi

export DATADIR=$DATADIR
sudo chmod 777 $DATADIR

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI
ip_address="10.195.52.144"
port="11000"


# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"/home/neutrino/FCT/code/scripts/FCT/Linux/one_channel_test/Script_one_ch.cs\",$SN,$channel)"
# Close all GUIs to avoid double serial com
sudo kill $(pidof mono)
# Open GUI and wait 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; sleep 11; } | telnet $ip_address $port 

# Close the GUI
sudo kill $(pidof mono)
sleep 1

file_name=$DATADIR"/one_ch_test_SN"$SN"_ch"$channel".daq"

$exe_path/OneChannelTest -f $file_name
echo "done"