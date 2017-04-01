# This script builds the Flame core libraries with dsc, and then
# proceeds to call xbuild on the peripheral Flame libraries.

bash ./BuildFlameCore.sh $@
cd ./Flame.Cecil/
xbuild /p:Configuration=Release /verbosity:minimal Flame.Cecil.sln
cd ..
