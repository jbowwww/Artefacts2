#
# Simple commit script
#

#Get a commit message
mv -f .commit_message_current.txt .commit_message_previous.txt
gedit -w .commit_message_current.txt
if [ ! -f ".commit_message_current.txt" ]; then
	echo ".commit_message_current.txt not found!";
	exit 1;
fi

#Add all files (and list)
git add -Av .

#Commit
git commit -m "`cat .commit_message_current.txt`"

#push all files (verbosely)
git push -v # --all

#Create tar backup
DATE=`date +%y%m%d-%H%M`;	#Get date string formatted for use in a backup filename
MSG=`cat .commit_message_current.txt | sed 's/ \(.\)\{1\}/\u\1/g'`
cd ..
tar -cvzf Backups/Artefacts-$DATE-$MSG.tar.gz Artefacts;
cd Artefacts

echo "Commit done.\n"
