import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d import Axes3D
from matplotlib import cm
from matplotlib import colors
import copy

DL = [0, 500, 1000, 2000, 2500, 3000]
ext='_c=40H=16'

# Extraction of results from .txt files #
fileTable = dict((key, 'mapTable_DL='+str(key)+ext+'.txt') for key in DL)
fileIso500 = dict((key, 'mapIso500_DL='+str(key)+ext+'.txt') for key in DL)
fileZone = dict((key, 'mapZone_DL='+str(key)+ext+'.txt') for key in DL)
fileAdvanced = dict((key, 'mapAdvanced_DL='+str(key)+ext+'.txt') for key in DL)

Table = dict((key, []) for key in DL)
XTable = dict((key, []) for key in DL)

Iso500 = dict((key, []) for key in DL)
XIso500 = dict((key, []) for key in DL)

Zone = dict((key, []) for key in DL)
XZone = dict((key, []) for key in DL)

Advanced = dict((key, []) for key in DL)
XAdvanced = dict((key, []) for key in DL)

for i, dl in enumerate(DL):
	for i, line in enumerate(open(fileTable[dl])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Table[dl].append(int(c))
				XTable[dl].append(200 + j*25)
	temp = copy.deepcopy(XTable[dl])
	XTable[dl] = list(reversed(Table[dl])) + XTable[dl]
	Table[dl] = list(reversed(temp)) + Table[dl]
	
	for i, line in enumerate(open(fileIso500[dl])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Iso500[dl].append(int(c))
				XIso500[dl].append(200 + j*25)
	temp = copy.deepcopy(XIso500[dl])
	XIso500[dl] = list(reversed(Iso500[dl])) + XIso500[dl]
	Iso500[dl] = list(reversed(temp)) + Iso500[dl]
				
	for i, line in enumerate(open(fileZone[dl])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Zone[dl].append(int(c))
				XZone[dl].append(200 + j*25)
	temp = copy.deepcopy(XZone[dl])
	XZone[dl] = list(reversed(Zone[dl])) + XZone[dl]
	Zone[dl] = list(reversed(temp)) + Zone[dl]
				
	for i, line in enumerate(open(fileAdvanced[dl])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Advanced[dl].append(int(c))
				XAdvanced[dl].append(200 + j*25)
	temp = copy.deepcopy(XAdvanced[dl])
	XAdvanced[dl] = list(reversed(Advanced[dl])) + XAdvanced[dl]
	Advanced[dl] = list(reversed(temp)) + Advanced[dl]

				
# Plot #
nbcols = 2
nbrows = 3
props = dict(boxstyle='round', facecolor='white', alpha=0.5)
fig , axes = plt.subplots(nrows=nbrows, ncols = nbcols)
for i in range(nbrows):
	for j in range(nbcols):
		if(nbcols*i+j < len(DL)):
			dl = DL[nbcols*i+j]
			axes[i,j].plot(XTable[dl],Table[dl],lw=2,color='blue',label='Table')
			axes[i,j].plot(XIso500[dl],Iso500[dl],lw=2,color='orange',label='Iso500')
			axes[i,j].plot(XZone[dl],Zone[dl],lw=2,color='green',label='Zone')
			axes[i,j].plot(XAdvanced[dl],Advanced[dl],lw=2,color='red',label='Advanced')
			axes[i,j].set_xlim(180,1220)
			axes[i,j].set_ylim(180,1220)
			axes[i,j].grid(True, linestyle='--', color='lightgray')
			if(i == nbrows - 1): axes[i,j].set_xlabel('LX (mm)')
			if(j == 0): axes[i,j].set_ylabel('LY (mm)')
			axes[i,j].text(200,1120,'DL = '+str(dl)+'kN', bbox=props)
			axes[i,j].legend()

fig.suptitle('SDL =500kN, LL=500kN, 3X3H16, cover = 40mm')
plt.show()
