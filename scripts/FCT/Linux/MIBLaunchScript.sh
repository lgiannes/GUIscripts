sn=$1


GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI
ip_address="10.195.52.144"
port="11000"

dummy_EOS="/MIB_"$sn"_EndOfScript.txt"
Data_path=$MIBDATADIR

# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"/home/neutrino/FCT/code/scripts/FCT/Linux/MIBtest.cs\",$sn)"
if [ -z $(pidof mono) ]
then 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1
fi

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command; bash wait.sh $Data_path$dummy_EOS; } | telnet $ip_address $port 

if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
    echo
fi