
GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI

source $FCT_RUN_FOLDER/setup.sh
command="Sync.RunScript(\"$FCT_UTILS//FEBenable.cs\")"

if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi
sleep 1
echo
# Open GUI and wait 
( cd $GUI_path && mono $GUI_path$GUI_exe & )

echo "Do nothing. wait 6 secs. (You may check that the GPIO is ON)"
sleep 2.5
echo "another 2 sec ..."


# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; sleep 2.5; } | telnet $ip_address $port 

# Close the GUI
if [ -z $(pidof mono) ]
then 
    echo
else
    echo
    sudo kill $(pidof mono)
    
fi
sleep 0.2
echo