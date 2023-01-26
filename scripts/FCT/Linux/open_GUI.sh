
if [ -z $(pidof mono) ]
then 
( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1
fi


( cd $GUI_path && mono $GUI_path$GUI_exe & )
echo "Opening GUI ..."
sleep 0.5
echo "When GUI is open, press Enter "
echo "(Close pop-up error windows on GUI, if any. DO NOT CLOSE THE SOCKET WINDOW! )"
read -n 1
