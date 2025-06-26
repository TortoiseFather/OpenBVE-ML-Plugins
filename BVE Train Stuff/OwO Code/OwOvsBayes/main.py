import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.naive_bayes import GaussianNB
from sklearn.metrics import confusion_matrix
from sklearn.metrics import accuracy_score

df = pd.read_csv('train_data_log.csv')
X = df.iloc[:, [1,2,4,5,7,9]].values
Y = df.iloc[:, 3].values
X_Train, X_Test, Y_Train, Y_Test = train_test_split(X, Y, test_size=0.33, random_state=2)
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

input_series1 = np.array([[0, 0, False, False, 9.7, 0]])
input_series = sc_X.transform(input_series1)  # Apply the same scaling as the training data
predicted_output = classifier.predict(input_series)

print("output for input series with OwO Naive Bayes: ", input_series1, " ", predicted_output)
print("Accuracy for OwO Bayes: ",accuracy)