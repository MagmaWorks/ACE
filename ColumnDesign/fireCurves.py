import matplotlib.pyplot as plt
import numpy as np


nb = 240
max = 240
X = np.linspace(0,max,nb+1)

Standard = np.zeros(nb+1)
External = np.zeros(nb+1)
Hydrocarbon = np.zeros(nb+1)

for i in range(nb+1):
	t = i*max/nb
	Standard[i] = 20 + 345*np.log10(8*t + 1)
	External[i] = 660*(1-0.687*np.exp(-0.32*t)-0.313*np.exp(-3.8*t))+20
	Hydrocarbon[i] = 1080*(1-0.325*np.exp(-0.167*t)-0.675*np.exp(-2.5*t))+20
	
plt.plot(X,Standard, color='red', label='standard')
plt.plot(X,External, color='blue', label='external')
plt.plot(X,Hydrocarbon, color='green', label='hydrocarbon')

plt.xlim(0,max)
plt.ylim(0)
plt.grid(True, linestyle='--', color='lightgray')
plt.legend()
plt.xlabel('Time (min)')
plt.ylabel('Temperature (\u00b0C)')
plt.title('Temperature curves')

plt.show()