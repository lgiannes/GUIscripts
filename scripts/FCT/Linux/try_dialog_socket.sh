

GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI
ip_address="10.195.52.144"
port="11000"



# Define the command to run the GUI script
command="Sync.RunScript(\"/home/neutrino/FCT/code/scripts/FCT/Linux/dialog.cs\")"
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
{ sleep 5; echo $command; sleep 1000; } | telnet $ip_address $port 

