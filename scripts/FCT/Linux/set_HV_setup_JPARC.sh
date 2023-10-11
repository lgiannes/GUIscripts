HV=$1

if [[ -z $1 ]]
then 
echo "-z $1"
HV="0"
fi

if [[ $1 -eq "OFF" ]]
then 
  echo "V1 0" > /dev/ttyACM1
  echo "V2 0" > /dev/ttyACM1
  echo "OP1 0" > /dev/ttyACM1
  echo "OP2 0" > /dev/ttyACM1
  return 0;
fi

#echo "SETTING HV TO $HV V"
  echo "V2 0" > /dev/ttyACM1
  echo "V1 $HV" > /dev/ttyACM1
  echo "OP1 1" > /dev/ttyACM1
  echo "OP2 0" > /dev/ttyACM1
  return 0;
