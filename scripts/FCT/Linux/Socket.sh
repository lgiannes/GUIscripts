#!/bin/bash

#Define variables that you use later
exe_path="/home/neutrino/FCT/FunctionalTest/bin/";
exe_analog="FuncTest";
exe_bl="FCTbaseline"
Data_path="/home/neutrino/FCT/data_local/";
Data_file_name="FCT_os_LG56HG12amp30mV_bl32786.daq";
Data_file_name_bl1="FCT_BLTEST_LG56HG12amp30mV_baseline32000.daq"
Data_file_name_bl2="FCT_BLTEST_LG56HG12amp30mV_baseline50000.daq"

setup_path="/home/neutrino/FCT/FunctionalTest/setup.sh";
log_file=$Data_path+"log.txt";
dummy_EOS="EndOfScript.txt"
SN_="SN_"
GUI_path="/home/neutrino/FCT/GUI_UT92/"
GUI_exe="UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI
ip_address="10.195.52.144"
port="12000"

#Kill all the jobs (avoid double serial communication)
kill $(jobs -p)

# Open GUI and wait 1 sec
( cd $GUI_path && mono $GUI_path$GUI_exe& )


# Ask the user for the FEB Serial Number
echo "Enter serial number:"
read sn

# Print out data folder and give rwe permission
Data_path=$Data_path$SN_$sn/
echo $Data_path
sudo chmod 777 $Data_path

# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"/home/neutrino/FCT/code/scripts/FCT/Linux/Script_FCT_openshort.cs\",$sn)"

#Remove the "EndOFScript.txt" dummy file if it exists already in the directory
if [[ -f $Data_path$dummy_EOS ]]
then 
read -p "\"Files already present for this SN. Do you want to overwrite? " -n 1 -r
echo    # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    rm -f $Data_path$dummy_EOS
else
sudo kill $(pidof mono)
sleep 1
exit
fi

fi

# Open the serial com and send the command. Leave it open for some seconds to show the 
# initial script log on terminal
{ sleep 1; echo $command; bash wait.sh $sn; }| telnet $ip_address $port 

# Close the GUI
sudo kill $(pidof mono)
sleep 1

#Launch the ROOT analyses
source $setup_path;
#Open/Short and Basic Analog
$exe_path$exe_analog -f $Data_path$Data_file_name -s$sn;
#Baseline test
$exe_path$exe_bl -a $Data_path$Data_file_name_bl1 -b $Data_path$Data_file_name_bl2 -s$sn;
