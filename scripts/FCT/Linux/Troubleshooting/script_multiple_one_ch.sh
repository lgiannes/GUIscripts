source $FCT_RUN_FOLDER/setup.sh

sn=$1
GUI_path=$GUI_FOLDER
GUI_exe="/UnigeGpioBoard.exe"
# Set the ip address of this machine and the port set for the GUI
echo >> dump.txt
echo "//////////////////////////////////////////////////////////////////////////////">> dump.txt
echo "Starting test on SN $sn" >> dump.txt
echo >> dump.txt
echo "//////////////////////////////////////////////////////////////////////////////">> dump.txt
# Open GUI and wait 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1


for i in {1..30}; do
echo "time: "$(date) >> dump.txt
bash one_channel_test/NOGUI_run_one_ch_test.sh $sn 224 >>dump.txt
done


# Close the GUI
if [ -z $(pidof mono) ]
then 
    echo
else
    sudo kill $(pidof mono)
fi