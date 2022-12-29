# -*- coding: utf-8 -*-
"""
just test something
@author: chhung
"""
import numpy as np
import pandas as pd
import plotly.graph_objects as go
import plotly.express as px

import dash
from dash.dependencies import Input, Output
import dash_html_components as html
import dash_core_components as dcc

import random

#%% pareto chart test
'''
def pareto_chart():
    data = []
    with open("satisfiction.txt", "r") as f:
        rawData = f.read().splitlines() 
    for i in rawData:
        data.append(int(i))
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
    #print(df)
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
#pareto_chart()
'''
#%% dynamic dcc layout test
'''
app = dash.Dash()
app.layout = html.Div([
    html.Button(id='button', n_clicks=0, children='Add graph'),
    html.Div(id='container'),
    #html.Div(dcc.Graph(id='empty', figure={'data': []}), style={'display': 'none'})
])

mOptions = [1, 2, 3]

@app.callback(Output('container', 'children'), [Input('button', 'n_clicks')])
def display_graphs(n_clicks):
    graphs = []
    dropdowns = []
    for i in range(n_clicks):
        dropdowns.append(dcc.Dropdown(options = mOptions, value = mOptions[0], clearable = True, id = 'cmpDropdown-{}'.format(i)))

        """
        graphs.append(dcc.Graph(
            id='graph-{}'.format(i),
            figure={
                'data': [{
                    'x': [1, 2, 3],
                    'y': [3, 1, 2]
                }],
                'layout': {
                    'title': 'Graph {}'.format(i)
                }
            }
        ))
        """
    return html.Div(dropdowns)

if __name__ == '__main__':
    app.run_server(debug=False)
'''
#%% two box plot test
path1 = "D:/ChengHao/thesisCode/Human_Simulation/Assets/StreamingAssets/Simulation_Result/noQuick2/"
path2 = "D:/ChengHao/thesisCode/Human_Simulation/Assets/StreamingAssets/Simulation_Result/noQuick3/"
path3 = "D:/ChengHao/thesisCode/Human_Simulation/Assets/StreamingAssets/Simulation_Result/noQuick/"
path = [path1, path2, path3]
dir1 = "sim1"
dir2 = "sim2"
dir3 = "sim3"
dir = [dir1, dir2, dir3]

filename = "status_time.txt"
def getStatusDataFrame(filename, dirName):
    df = pd.read_csv(filename, sep = ' ', header = None)
    df.columns = ["status", "time", "id", "humantype"]
    d = []
    [d.append(dirName) for i in range(len(df.index))]
    df["dirName"] = d  
    return df


def compareStatus():
    dfList = []
    for i in range(len(path)):
        df = getStatusDataFrame(path[i] + filename, dir[i])
        dfList.append(df)
    df = dfList[0]
    for i in range(1, len(path)):
        df = pd.concat([df, dfList[i]], ignore_index = True)
    print(df)
    #fig = px.box(df, x = "dirName", y = "time", color = "status", points = "all")
    fig = px.box(df, x = "status", y = "time", color = "dirName", points = "outliers")
    fig.update_layout(yaxis_title = "time (sec)")
    fig.show()

compareStatus()

# %%
filename = "visiting_time.txt"
def getVisitingTimeDataFrame(filename, dirName):
    df = pd.read_csv(filename, sep = ' ', header = None)
    columnNames = []
    [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns) - 2)]
    columnNames.append('id')
    columnNames.append('humantype')
    df.columns = columnNames
    #handle data
    colNames = ['exhibition', 'time', 'id', 'humantype']
    new_df = pd.DataFrame(columns = colNames)
    for j in range(len(df.index)):
        for k in range(len(df.columns) - 2):
            exhibitionName = 'p' + str(k+1)
            add_df = pd.DataFrame({'exhibition': exhibitionName + ' ' + dirName, 'time': df.loc[j, exhibitionName], 
                                    'id': df.loc[j, 'id'], 'humantype': df.loc[j, 'humantype']}, index = [0])
            new_df = pd.concat([new_df, add_df], ignore_index = True)

    d = []
    [d.append(dirName) for i in range(len(new_df.index))]
    new_df["dirName"] = d
    return new_df

def compareVisitingTime():
    dfList = []
    for i in range(len(path)):
        df = getVisitingTimeDataFrame(path[i] + filename, dir[i])
        dfList.append(df)
    df = dfList[0]
    for i in range(1, len(path)):
        df = pd.concat([df, dfList[i]], ignore_index = True)
    
    #fig = px.box(df, x = "exhibition", y = "time", color = "dirName", points = "all")
    #fig = px.box(df, x = "exhibition", y = "time", color = "dirName", boxmode = "group", points = "all")
    #fig = px.box(df, x = "dirName", y = "time", color = "exhibition", points = "all") #1

    colorList = ["#ff0000", "#ff7b00", "#7bff00", "#00ffdd", "#0800ff", "#ff00ff"]
    color = random.sample(colorList, len(path) * 2)
    xLabelList = []
    for i in range(1, 6):
        for j in range(len(path)):
            xLabelList.append('p' + str(i) + ' ' + dir[j])

    fig = go.Figure()
    for i in range(len(path)):
        df = dfList[i]
        fig.add_traces([go.Box(x = df["exhibition"], y = df["time"], marker_color = color[i], name = dir[i], pointpos = 0),
                        go.Scatter(x = df["exhibition"], y = df["time"], mode = "markers",marker_color = color[i+3], name = dir[i])
                      ])

    fig.update_layout(template='plotly_white', showlegend=True, hovermode='x', boxgap=0.1,
                      title= {'text': 'visitors\' satisfiction', 'x': .5}, 
                      yaxis= {'title': 'time (sec)'},
                      )

    print(xLabelList)
    fig.update_xaxes( categoryorder = 'array', 
                      categoryarray= xLabelList)

    fig.update_layout(
        xaxis = dict(
            tickmode = 'array',
            tickvals = np.arange(0, 15),
            ticktext = ['', 'p1', '', '', 'p2', '', '', 'p3', '', '', 'p4', '', '', 'p5', '']
        )
    )
    #fig.update_layout(yaxis_title = "time (sec)")
    fig.show()

#compareVisitingTime()

#%% multi realtime test now fail
filename = "ex_realtime_human_count.txt"
def getExhibitRealtimeVisitorCountDataFrame(filename, dirName):
    df = pd.read_csv(filename, sep = ' ', header = None)
    df = df.iloc[: , :-1] # remove the last data (NaN)
    df = df.T
    columnNames = []
    [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns))]
    df.columns = columnNames

    columnNames = ['exhibit', 'time', 'count', 'dirName']
    new_df = pd.DataFrame(columns = columnNames)
    for i in range(len(df.columns)):
        for j in range(len(df.index)):
            exhibitName = 'p' + str(i + 1)
            add_df = pd.DataFrame({'exhibit': exhibitName, 'time': j, 
                                   'count': df.loc[j, exhibitName], 'dirName': dirName}, index= [0])
            new_df = pd.concat([new_df, add_df], ignore_index = True)
    return new_df

def compareExhibitRealtimeVisitorCount():
    dfList = []
    for i in range(len(path)):
        df = getExhibitRealtimeVisitorCountDataFrame(path[i] + filename, dir[i])
        dfList.append(df)
    df = dfList[0]
    for i in range(1, len(path)):
        df = pd.concat([df, dfList[i]], ignore_index = True)
    
    fig = px.line(df, x= "time", y = "count", color = "dirName",line_group = "exhibit", title = "Real-time visitor number in the exhibit")
    fig.update_layout(xaxis_title = "time (sec)", yaxis_title = "number of visitor", legend = dict(title = "exhibit"))
    fig.show()

#compareExhibitRealtimeVisitorCount() #fail

#%% multi pareto test

filename = "satisfiction.txt"
def getSatisfictionDatFrame(filename, dirName):
    data = []
    with open(filename, "r") as f:
        rawData = f.read().splitlines() 
    for i in rawData:
        data.append(int(i))
    df = pd.DataFrame(data, index = ["100%", "75~100%", "50~75%", "25~50%", "0~25%"], columns = ["count"])
    total = df["count"].sum()
    sum = 0
    d = []
    ratio = []
    for i in range(len(data)):
        sum = sum + (data[i] / total)
        ratio.append(sum)
        d.append(dirName)
    
    df["ratio"] = ratio
    df["dirName"] = d
    return df

def compareSatisfiction():
    dfList = []
    for i in range(len(path)):
        df = getSatisfictionDatFrame(path[i] + filename, dir[i])
        dfList.append(df)

    #color = ["#"+''.join([random.choice('0123456789ABCDEF') for j in range(6)]) for i in range(len(path))]
    colorList = ["#ff0000", "#ff7b00", "#7bff00", "#00ffdd", "#0800ff", "#ff00ff"]
    color = random.sample(colorList, len(path))

    fig = go.Figure()
    for i in range(len(path)):
        df = dfList[i]
        fig.add_traces([go.Bar( x = df.index, y = df["count"], marker_color = color[i], yaxis='y1', name=dir[i]),
                        go.Scatter( x=df.index, y=df['ratio'], marker_color = color[i], yaxis='y2', name=dir[i] + ' cumulative ratio', hovertemplate='%{y:.1%}', marker={'color': '#000000'})
                       ])

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
#compareSatisfiction()

def color_variant(hex_color, brightness_offset=1):
    """ takes a color like #87c95f and produces a lighter or darker variant """
    if len(hex_color) != 7:
        raise Exception("Passed %s into color_variant(), needs to be in #87c95f format." % hex_color)
    rgb_hex = [hex_color[x:x+2] for x in [1, 3, 5]]
    new_rgb_int = [int(hex_value, 16) + brightness_offset for hex_value in rgb_hex]
    new_rgb_int = [min([255, max([0, i])]) for i in new_rgb_int] # make sure new values are between 0 and 255
    # hex() produces "0x88", we want just "88"
    return "#" + "".join([hex(i)[2:] for i in new_rgb_int])