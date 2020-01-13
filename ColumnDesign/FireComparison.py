import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d import Axes3D
from matplotlib import cm
from matplotlib import colors
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
from scipy.spatial import ConvexHull

file1 = open("mapTable.txt")
file2 = open("mapIso500.txt")
file4 = open("mapZone.txt")
file3 = open("mapAdvanced.txt")
fileDim = open("secondDim.txt")
fileLoads = open("axialLoads.txt")

X = np.linspace(200,600,21)
Y = np.linspace(200,600,17)

Table = np.zeros((17, 21))
Iso500 = np.zeros((17, 21))
Zone = np.zeros((17, 21))
Advanced = np.zeros((17, 21))
SecondDim = np.zeros(17)
Areas = np.zeros(17)
ULS = np.zeros(21)
fireComb = np.zeros(21)

#print(Table)

for i, line in enumerate(file1):
	columns = line.split()
	for j, c in enumerate(columns):
		Table[j,i] = c
		
for i, line in enumerate(file2):
	columns = line.split()
	for j, c in enumerate(columns):
		Iso500[j,i] = c

for i, line in enumerate(file3):
	columns = line.split()
	for j, c in enumerate(columns):
		Advanced[j,i] = c
		
for i, line in enumerate(file4):
	columns = line.split()
	for j, c in enumerate(columns):
		Zone[j,i] = c

fig , axes = plt.subplots(nrows=2, ncols = 2)
cmap1 = colors.ListedColormap(['white', 'blue'])
cmap2 = colors.ListedColormap(['white', 'green'])
cmap3 = colors.ListedColormap(['white','red'])
cmap4 = colors.ListedColormap(['white','orange'])
axes[0,0].contourf(X, Y, Table, cmap = cmap1, alpha = 0.5, levels = 1)
axes[0,0].contour(X, Y, Table, levels = [0.5], colors = 'blue', linewidths=1)
axes[0,0].contourf(X, Y, Iso500, cmap = cmap2, alpha = 0.5, levels = 1)
axes[0,0].contour(X, Y, Iso500, levels = [0.5], colors = 'green', linewidths=1)
axes[0,0].contourf(X, Y, Advanced, cmap = cmap3, alpha = 0.5, levels = 1)
axes[0,0].contour(X, Y, Advanced, levels = [0.5], colors = 'red', linewidths=1)
axes[0,0].contourf(X, Y, Zone, cmap = cmap4, alpha = 0.5, levels = 1)
axes[0,0].contour(X, Y, Zone, levels = [0.5], colors = 'orange', linewidths=1)
axes[0,0].set_xlabel('SDL (kN)')
axes[0,0].set_ylabel('h (mm)')

proxy = [plt.Rectangle((0,0),1,1,fc = 'blue'),
		 plt.Rectangle((0,0),1,1,fc = 'green'),
		 plt.Rectangle((0,0),1,1,fc = 'orange'),
		 plt.Rectangle((0,0),1,1,fc = 'red')]

axes[0,0].legend(proxy, ['Table', 'Iso500', 'Zone method', 'Advanced'])

for i, line in enumerate(fileDim):
	columns = line.split()
	if (i == 0):
		for j, c in enumerate(columns):
			SecondDim[j] = c
	elif (i == 1):
		for j, c in enumerate(columns):
			Areas[j] = c
			
ax2 = axes[1,0].twinx()
axes[1,0].plot(Y,SecondDim,lw=2,color='blue', label = 'b')
ax2.plot(Y,Areas,lw=2,color='orange', label = 'sec')
axes[1,0].set_xlabel('h (mm)')
axes[1,0].set_ylabel('b (mm)')
axes[1,0].grid(True, linestyle='--', color='lightgray')
ax2.set_ylabel('Section (cm2)')
ax2.set_ylim(10,14)
axes[1,0].legend()
axes[1,0].text(200,200,'Ref section = 350x350 \nRebars = 3x3 H16')

for i, line in enumerate(fileLoads):
	columns = line.split()
	if (i == 0):
		for j, c in enumerate(columns):
			ULS[j] = c
	elif (i == 1):
		for j, c in enumerate(columns):
			fireComb[j] = c

axes[0,1].plot(X,ULS,lw=2,color='g', label='0.7*ULS')		
axes[0,1].plot(X,fireComb,lw=2,color='r', label='DL+SDL+0.5LL')
axes[0,1].grid(True, linestyle='--', color='lightgray')
axes[0,1].set_xlim(200,600)
axes[0,1].set_ylim(0,3400)
axes[0,1].legend()
axes[0,1].set_xlabel('SDL (kN)')
axes[0,1].set_ylabel('Axial Loads (kN)')
axes[0,1].text(220,100,'DL = 1500kN \n LL = 500kN')

fig.delaxes(axes[1,1])

plt.show()