for sysdevpath in $(find /sys/bus/usb/devices/usb*/ -name ttyACM*); 
do     (
    syspath="${sysdevpath%/dev}"; 
    devname="$(udevadm info -q name -p $syspath)";
    [[ "$devname" == "bus/"* ]] && exit;
    eval "$(udevadm info -q property --export -p $syspath)";
    echo "/dev/$devname - $ID_SERIAL";
);
done 
