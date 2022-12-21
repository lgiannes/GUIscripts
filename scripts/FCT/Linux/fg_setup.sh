#! /bin/bash

freq=200 #Hz
pulseper=1/$freq #s
amp=0.03 #V
rise=10E-9 #s
fall=99E-9 #s
width=1E-6 #s

# #device=/dev/serial/by-id/usb-THURLBY_THANDAR_TG5011_55126528-if00
# device=$( tio -L[0] )
# #echo $device
# if [ -z "$device" ];
# then echo "error: no device connected"
# else
# echo $device
# tio ${device} --response-timeout 2000
# echo "OUTPUT ON;WAVE PULSE;AMPL $amp;PULSFREQ $freq;PULSWID $width;PULSRISE $rise;PULSFALL $fall" | tio ${device} --response-timeout 2000
# sleep 3
# echo "OUTPUT OFF" | tio ${device} --response-timeout 2000
# sleep 3
# fi


echo "WAVE PULSE"     | cat > /dev/ttyACM0
sleep 0.1
echo "AMPL $amp"      | cat > /dev/ttyACM0
sleep 0.1
echo "PULSFREQ $freq" | cat > /dev/ttyACM0
sleep 0.1
echo "PULSWID $width" | cat > /dev/ttyACM0
sleep 0.1
echo "PULSRISE $rise" | cat > /dev/ttyACM0
sleep 0.1
echo "PULSFALL $fall" | cat > /dev/ttyACM0
sleep 0.1
echo "PULSDELAY 0"     | cat > /dev/ttyACM0
sleep 0.1
echo "DCOFFS 0"     | cat > /dev/ttyACM0
sleep 0.1
