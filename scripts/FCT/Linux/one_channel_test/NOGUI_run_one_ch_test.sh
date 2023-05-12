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



# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/one_channel_test/Script_one_ch.cs\",$SN,$channel)"

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; sleep 13; } | telnet $ip_address $port 

file_name=$GENERALDATADIR"FEBs/SN_"$SN"/Single_Channels_Tests/one_ch_test_SN"$SN"_ch"$channel".daq"

$exe_path/OneChannelTest -f $file_name
echo "done"