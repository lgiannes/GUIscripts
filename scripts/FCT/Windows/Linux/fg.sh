#! /bin/bash

if [ $1 == 0 ]
then switch='OFF'

elif [ $1 == 1 ]
then switch='ON'

else echo "type 0 turn off input, 1 to turn on"
fi


#device=/dev/serial/by-id/usb-THURLBY_THANDAR_TG5011_55126528-if00
device=$( tio -L[0] )
echo $device
if [ -z "$device" ];
then echo "error: no device connected"
else
#echo $device
echo "OUTPUT $switch" | tio ${device} --response-timeout 2000
fi