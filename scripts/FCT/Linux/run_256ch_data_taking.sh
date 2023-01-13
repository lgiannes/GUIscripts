sn=$1
bl1=$2
bl2=$3

GUI_path="/home/neutrino/FCT/GUI_UT92/"
GUI_exe="UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI
ip_address="10.195.52.144"
port="11000"

dummy_EOS="EndOfScript.txt"
dummy_EOS_citi="EndOfScript_citi.txt"
Data_path=$DATADIR
echo "DATADIR: "$DATADIR

# Define the command to run the GUI script
command="Sync.RunScriptArgs(\"/home/neutrino/FCT/code/scripts/FCT/Linux/Script_FCT_openshort.cs\",$sn,$bl1,$bl2)"
command_citi="Sync.RunScriptArgs(\"/home/neutrino/FCT/code/scripts/FCT/Linux/Script_FCT_CITI_test.cs\",$sn)"
# Close all GUIs to avoid double serial com
sudo kill $(pidof mono)
# Open GUI and wait 
( cd $GUI_path && mono $GUI_path$GUI_exe& )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1

# Open the serial com and send the command. Wait for it to end. Send second command, wait for it to end an close serial port com
{ sleep 1; echo $command_citi; bash wait.sh $Data_path$dummy_EOS_citi; }| telnet $ip_address $port 

# sleep 1; echo $command; bash wait.sh $Data_path$dummy_EOS; 

# Close the GUI
sudo kill $(pidof mono)
sleep 1
