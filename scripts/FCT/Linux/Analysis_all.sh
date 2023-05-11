if [[ $# -ne 2 ]] ;then 
    echo Provide 2 SNs, will do analysis of all SNs between first and second argument
    exit 
else 
    if [[ $1 > $2 ]]; then
        echo "ERROR: seocnd argument must be larger!"
        exit
    else
        sn=$1
        while [ "$2" -ge "$sn" ] 
        do
            echo "ANALYZING SN $sn"
            bash Analysis_only.sh $sn
            sn=$((sn+1))
        done 
    
    fi
fi