import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d import Axes3D
from matplotlib import cm
from matplotlib import colors
import copy

MX = [50, 100, 250, 500]
ext='_c=40H=16'

# Extraction of results from .txt files #
fileTable = dict((key, 'mapTable_Mx='+str(key)+ext+'.txt') for key in MX)
fileIso500 = dict((key, 'mapIso500_Mx='+str(key)+ext+'.txt') for key in MX)
fileZone = dict((key, 'mapZone_Mx='+str(key)+ext+'.txt') for key in MX)
fileAdvanced = dict((key, 'mapAdvanced_Mx='+str(key)+ext+'.txt') for key in MX)

Table = dict((key, []) for key in MX)
XTable = dict((key, []) for key in MX)

Iso500 = dict((key, []) for key in MX)
XIso500 = dict((key, []) for key in MX)

Zone = dict((key, []) for key in MX)
XZone = dict((key, []) for key in MX)

Advanced = dict((key, []) for key in MX)
XAdvanced = dict((key, []) for key in MX)

for i, mx in enumerate(MX):
	for i, line in enumerate(open(fileTable[mx])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Table[mx].append(int(c))
				XTable[mx].append(200 + j*25)
	# temp = copy.deepcopy(XTable[mx])
	# XTable[mx] = list(reversed(Table[mx])) + XTable[mx]
	# Table[mx] = list(reversed(temp)) + Table[mx]
	
	for i, line in enumerate(open(fileIso500[mx])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Iso500[mx].append(int(c))
				XIso500[mx].append(200 + j*25)
	# temp = copy.deepcopy(XIso500[mx])
	# XIso500[mx] = list(reversed(Iso500[mx])) + XIso500[mx]
	# Iso500[mx] = list(reversed(temp)) + Iso500[mx]
				
	for i, line in enumerate(open(fileZone[mx])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Zone[mx].append(int(c))
				XZone[mx].append(200 + j*25)
	# temp = copy.deepcopy(XZone[mx])
	# XZone[mx] = list(reversed(Zone[mx])) + XZone[mx]
	# Zone[mx] = list(reversed(temp)) + Zone[mx]
				
	for i, line in enumerate(open(fileAdvanced[mx])):
		col = line.split()
		k = 0
		for j, c in enumerate(col):
			if(int(c) != 0):
				Advanced[mx].append(int(c))
				XAdvanced[mx].append(200 + j*25)
	# temp = copy.deepcopy(XAdvanced[mx])
	# XAdvanced[mx] = list(reversed(Advanced[mx])) + XAdvanced[mx]
	# Advanced[mx] = list(reversed(temp)) + Advanced[mx]

				
# Plot #
props = dict(boxstyle='round', facecolor='white', alpha=0.5)
nbrows = 2
nbcols = 2
fig , axes = plt.subplots(nrows=nbrows, ncols = nbcols)
for i in range(nbrows):
	for j in range(nbcols):
		if(nbcols*i+j < len(MX)):
			mx = MX[nbcols*i+j]
			axes[i,j].plot(XTable[mx],Table[mx],lw=2,color='blue',label='Table')
			axes[i,j].plot(XIso500[mx],Iso500[mx],lw=2,color='orange',label='Iso500')
			axes[i,j].plot(XZone[mx],Zone[mx],lw=2,color='green',label='Zone')
			axes[i,j].plot(XAdvanced[mx],Advanced[mx],lw=2,color='red',label='Advanced')
			axes[i,j].set_xlim(180,1220)
			axes[i,j].set_ylim(180,1220)
			axes[i,j].grid(True, linestyle='--', color='lightgray')
			if(i == nbrows - 1): axes[i,j].set_xlabel('LX (mm)')
			if(j == 0): axes[i,j].set_ylabel('LY (mm)')
			axes[i,j].text(220,1120,'MX = '+str(mx)+'kN.m', bbox=props)
			axes[i,j].legend()

fig.suptitle('DL = 1000kN, SDL =500kN, LL=500kN, 3X3H16, cover = 40mm')
plt.show()
