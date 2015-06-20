sudo clear; sudo mongod --smallfiles -f /etc/mongodb.conf & sleep 2; ps -A | grep mongod; sudo tail -n 40 -f /var/log/mongodb/mongodb.log
