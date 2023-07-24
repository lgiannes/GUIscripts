echo
for sysdevpath in $(find /sys/bus/usb/devices/usb*/ -name ttyACM0); 
do     (
    syspath="${sysdevpath%/dev}"; 
    devname="$(udevadm info -q name -p $syspath)";
    [[ "$devname" == "bus/"* ]] && exit;
    eval "$(udevadm info -q property --export -p $syspath)";
    echo "/dev/$devname - $ID_SERIAL";
);
done 
sleep 1.5
echo
echo "If in the line above you don't read:"
echo "\"/dev/ttyACM0 - THURLBY_THANDAR_TG5011_55126528\""
echo "the pulse generator is not correctly working"
echo 

# Possible troubleshooting: 
# remove the device file /dev/ttyACM0: $ sudo rm /dev/ttyACM0
# then disconnect and reconnect the pulse generator 