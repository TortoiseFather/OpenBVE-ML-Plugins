#This code showcases the difference between using a standard NB standardscalar with
#a manual scalar discovered through the OwO method, the OwO method shows up to a 5%
#increase, depending on data size. The basic dataset shows a ~3% increase in accuracy
#over the standard scalar, with the increase becoming 5% if tracking power
import numpy as np
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.naive_bayes import GaussianNB
from sklearn.metrics import confusion_matrix
from sklearn.metrics import accuracy_score

df = pd.read_csv('train_data_log.csv')
X = df.iloc[:, [1,3,4,5,7,9]].values
Y = df.iloc[:, 2].values
X_Train, X_Test, Y_Train, Y_Test = train_test_split(X, Y, test_size=0.33, random_state=2)
sc_X = StandardScaler()
X_Train = sc_X.fit_transform(X_Train)
X_Test = sc_X.transform(X_Test)
classifier = GaussianNB()
classifier.fit(X_Train, Y_Train)
Y_Pred = classifier.predict(X_Test)
cm = confusion_matrix(Y_Test, Y_Pred)
accuracy =accuracy_score(Y_Test, Y_Pred)
print("Predicting power using NB")
print("Overall Accuracy: ", accuracy)
#print("Confusion Matrix: ", cm)
#print("Y axis: ", Y_Pred)
#display if you want.

#Starting OwO

input_series1 = np.array([[0, 0, False, False, 9.7, 0]])
input_series = sc_X.transform(input_series1)  # Apply the same scaling as the training data
predicted_output = classifier.predict(input_series)

weights = np.array([1.35, 1.5, 1.5, 0.2, 0.01, 0.1])

df = pd.read_csv('train_data_log.csv')
X = df.iloc[:, [1,3,4,5,7,9]].values
Y = df.iloc[:, 2].values
X_Train, X_Test, Y_Train, Y_Test = train_test_split(X, Y, test_size=0.33, random_state=2)
X_Train = X_Train * weights
X_Test = X_Test * weights

classifier = LogisticRegression()
classifier.fit(X_Train, Y_Train)
Y_Pred = classifier.predict(X_Test)
cm = confusion_matrix(Y_Test, Y_Pred)
accuracy =accuracy_score(Y_Test, Y_Pred)
print("Predicting using OwO")
print("Overall Accuracy: ", accuracy)
#print("Confusion Matrix: ", cm)
#print("Y axis: ", Y_Pred)
#display if you want.

#OwO on power
X = df.iloc[:, [1,2,4,5,7,9]].values
Y = df.iloc[:, 3].values
X_Train, X_Test, Y_Train, Y_Test = train_test_split(X, Y, test_size=0.33, random_state=2)
X_Train = X_Train * weights
X_Test = X_Test * weights
classifier.fit(X_Train, Y_Train)
Y_Pred = classifier.predict(X_Test)
cm = confusion_matrix(Y_Test, Y_Pred)
accuracy =accuracy_score(Y_Test, Y_Pred)
print("Predicting using OwO on the power")
print("Overall Accuracy: ", accuracy)

#to use, feed predicted_output into the simulator, currently struggles at real-time operattion
#due to processing requirements (needs threading which the version of C# the sim runs on doesn't allow)