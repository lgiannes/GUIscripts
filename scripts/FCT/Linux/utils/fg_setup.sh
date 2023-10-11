#! /bin/bash

freq=1000 #Hz
pulseper=1.0/$freq #s
## the amplitude shoul dbe set already
#echo "Amplitude = $amp V (cross check pulse generator)"
rise="10E-9" #s
fall=99E-9 #s
width=1E-6 #s

# #device=/dev/serial/by-id/usb-THURLBY_THANDAR_TG5011_55126528-if00
# device=$( tio -L[0] )
# #echo $device
# if [ -z "$device" ];
# then 
# echo "error: no device connected. Connect pulse generator"
# exit
# else
# echo $device
# tio ${device} --response-timeout 2000
# fi

echo "CHN 1"     | cat > /dev/ttyACM0
echo "CHN?"      | cat > /dev/ttyACM0

# echo "setting wave=pulse"
echo "WAVE PULSE"     | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting amplitude=$amp V"
echo "AMPUNIT VPP"      | cat > /dev/ttyACM0
echo "AMPL $amp"      | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting frequency=$freq Hz"
echo "PULSFREQ $freq" | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting width=$width s"
echo "PULSWID $width" | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting rising edge=$rise s"
echo "PULSRISE $rise" | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting falling edge=$fall s"
echo "PULSFALL $fall" | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting delay=0 s"
echo "PULSDLY 0"    | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
# echo "setting offset=0 s"
echo "DCOFFS 0"       | cat > /dev/ttyACM0
sleep 0.3
# echo "done"
