sn=$1
bl1=$2
bl2=$3

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI


dummy_EOS_citi="EndOfScript_CITIonly.txt"
Data_path=$DATADIR
# echo "DATADIR: "$DATADIR

# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/Script_FCT_openshort.cs\",$sn,$bl1,$bl2)"
command_citi="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/Script_FCT_CITI_test.cs\",$sn)"
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
{ sleep 1; echo $command_citi; bash wait.sh $Data_path$dummy_EOS_citi; } | telnet $ip_address $port 

# Close the GUI
if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi