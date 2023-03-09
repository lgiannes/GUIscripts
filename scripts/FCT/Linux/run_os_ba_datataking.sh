sn=$1
bl1=$2
bl2=$3

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI

dummy_EOS="EndOfScript_256only.txt"
Data_path=$DATADIR
echo "DATADIR: "$DATADIR

# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"$FCT_RUN_FOLDER/Script_FCT_256ch_only.cs\",$sn,$bl1,$bl2)"
# Close all GUIs to avoid double serial com
if [ -z $(pidof mono) ]
then 
  echo
else
  echo "Terminating previously open GUI..."
  sudo kill $(pidof mono)
  sleep 0.1
  echo
fi
# Open GUI and wait 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; bash wait.sh $Data_path$dummy_EOS; } | telnet $ip_address $port 

# sleep 1; echo $command; bash wait.sh $Data_path$dummy_EOS;

# Close the GUI
if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi

# erase EOS file
sudo rm -f $Data_path$dummy_EOS