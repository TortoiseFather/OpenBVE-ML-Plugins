import numpy as np
import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.naive_bayes import GaussianNB
from sklearn.metrics import confusion_matrix
from sklearn.metrics import accuracy_score

df = pd.read_csv('train_data_log.csv')
X = df.iloc[:, [1,3,4,5,7,8]].values
Y = df.iloc[:, 2].values
X_Train, X_Test, Y_Train, Y_Test = train_test_split(X, Y, test_size = 0.25, random_state = 0)
sc_X = StandardScaler()
X_Train = sc_X.fit_transform(X_Train)
X_Test = sc_X.transform(X_Test)
classifier = GaussianNB()
classifier.fit(X_Train, Y_Train)
Y_Pred = classifier.predict(X_Test)
cm = confusion_matrix(Y_Test, Y_Pred)
accuracy =accuracy_score(Y_Test, Y_Pred)
print("Overall Accuracy: ", accuracy)
print("Confusion Matrix: ", cm)
print("Y axis: ", Y_Pred)