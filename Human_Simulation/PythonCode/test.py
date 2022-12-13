# -*- coding: utf-8 -*-
"""
just test something
@author: chhung
"""
import numpy as np
import pandas as pd
import plotly.graph_objects as go

data = np.random.choice(['USA', 'Canada', 'Russia', 'UK', 'Belgium',
                                'Mexico', 'Germany', 'Denmark'], size=100,
                                 p=[0.43, 0.14, 0.23, 0.07, 0.04, 0.01, 0.03, 0.05])

#data = pd.read_csv("satisfiction.txt", header = None)

data = []
with open("satisfiction.txt", "r") as f:
     rawData = f.read().splitlines() 

for i in rawData:
    data.append(int(i))

def pareto_chart(collection):
    #collection = pd.Series(collection)
    df = pd.DataFrame(data, index = ["100%", "75~100%", "50~75%", "25~50%", "0~25%"], columns = ["count"])
    total = df["count"].sum()
    sum = 0
    ratio = []
    for i in range(len(data)):
        print(i)
        sum = sum + (data[i] / total)
        ratio.append(sum)
    
    df["ratio"] = ratio
    # drop count value = 0
    df = df[df["count"] != 0]
    print(df)

    
    fig = go.Figure([go.Bar(x=df.index, y=df["count"], yaxis='y1', name='visitor ount'),
                     go.Scatter(x=df.index, y=df['ratio'], yaxis='y2', name='cumulative ratio',
                                hovertemplate='%{y:.1%}', marker={'color': '#000000'})])

    fig.update_layout(template='plotly_white', showlegend=True, hovermode='x', bargap=.3,
                      title= {'text': 'visitors\' satisfiction', 'x': .5}, 
                      yaxis= {'title': 'count'},
                      yaxis2={'showgrid': False,
                              'rangemode': "tozero", 
                              'overlaying': 'y',
                              'position': 1, 'side': 'right',
                              'title': 'ratio',
                              'tickvals': np.arange(0, 1.1, .2),
                              'tickmode': 'array',
                              'ticktext': [str(i) + '%' for i in range(0, 101, 20)]})
                              
    
    fig.show()
    

pareto_chart(data)