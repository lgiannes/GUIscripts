  
  echo    "/--------------------------------------\\"
  echo    "|                                      |"
  echo    "|       Go on with other tests?        |" 
  echo    "|      (y=yes, any other key=no)       |" 
  echo    "|                                      |"
  read -p "\\--------------------------------------/" -n 1 -r -t 5 
  timeout=$?
  echo 
  if [[ $REPLY =~ ^[Yy\r]$ ]] 
  then
    echo "Starting other tests ..."
  elif [[ "$timeout" -gt 128 ]]
  then
    echo "Starting other tests ..."
  else
    echo "going out"
    exit
  fi  
