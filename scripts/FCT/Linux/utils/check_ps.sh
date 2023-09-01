echo
for sysdevpath in $(find /sys/bus/usb/devices/usb*/ -name ttyACM1); 
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
echo "\"/dev/ttyACM1 - TTi_MX_Series_PSU_491122\""
echo "the power supply is not correctly working"
echo 

echo "troubleshooting:" 
echo "remove the device file /dev/ttyACM1: sudo rm /dev/ttyACM1 "
echo "do the same for /dev/ttyACM0: sudo rm /dev/ttyACM0"
echo "then disconnect and reconnect the pulse generator" 
echo 