echo
  echo    "/--------------------------------------\\"
  echo    "|                                      |"
  echo    "|       Go on with other tests?        |" 
  echo    "|      (y=yes, any other key=no)       |" 
  echo    "|                                      |"
  read -p "\\--------------------------------------/" -n 1 -r 
  echo
  if [[ $REPLY =~ ^[\r]$ ]]
  then
    echo "Starting other tests ..."
  else
    echo "exit"
    exit
  fi  