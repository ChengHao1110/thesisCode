"""
compare function for dashboard
@author: chhung
"""
from dash import dcc, html
import dash_bootstrap_components as dbc

import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import numpy as np
import os
from PIL import Image
import random

import chord_diagram as cd
import dashboard_layout as db_lt

#region parameters
picCountInARow = 3

pathList = []
dirNameList = []
colorList = [
    '#1f77b4',  # muted blue
    '#ff7f0e',  # safety orange
    '#2ca02c',  # cooked asparagus green
    '#d62728',  # brick red
    '#9467bd',  # muted purple
    '#8c564b',  # chestnut brown
    '#e377c2',  # raspberry yogurt pink
    '#7f7f7f',  # middle gray
    '#bcbd22',  # curry yellow-green
    '#17becf'   # blue-teal
    ]
#endregion

#region function definition
def CardContent(partial):
    contents = []
    for i in range(len(partial)):
        contents.append(
            dbc.Col(
                partial[i]
            , width = 12 / picCountInARow)
        )

    partialLayout = dbc.Row([
               contents
            ]
            ,className="g-0")
    return partialLayout

def SetFigureTemplate(fig):
    fig.update_layout(template='plotly_dark',
                      plot_bgcolor= 'rgba(0, 0, 0, 0)',
                      paper_bgcolor= 'rgba(0, 0, 0, 0)',
                      margin={
                          "pad": 0,
                          "t": 0,
                          "r": 20,
                          "l": 20,
                          "b": 0,
                      },
                      autosize=True
                     )
    return fig

def HideXYAxisTicks(fig):
    fig.update_xaxes(showticklabels = False)
    fig.update_yaxes(showticklabels = False)
    return fig

def GetPicture(filename):
    img = np.array(Image.open(filename))
    fig = px.imshow(img)
    fig = SetFigureTemplate(fig)
    fig = HideXYAxisTicks(fig)
    return fig

def CompareLayout():
    # get all layout figures
    cards = []
    idx = 1
    for path , dirName in zip(pathList, dirNameList):
        figure = GetPicture(path + "layout_screenshot.png")
        fig_id = "layout_" + str(idx)
        content = db_lt.DrawCompareFigure(dirName, fig_id, figure)
        cards.append(content)
        idx = idx + 1

    layout = html.Div(
        dbc.CardGroup(
            cards
        )
    )

    return layout
        
def GetChordDiagram(filename):
    df = pd.read_csv(filename, sep = ' ', header = None)
    df = df.iloc[: , :-1] # remove the last data (NaN)
    df = df.T
    columnNames = []
    [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns) - 2)] #remove 2 exit
    columnNames.append('exit1')
    columnNames.append('exit2')
    df.columns = columnNames
    fig = cd.make_filled_chord(df)
    return fig

def CompareChordDiagram():
    cards = []
    idx = 1
    for path , dirName in zip(pathList, dirNameList):
        figure = GetChordDiagram(path + "ex_trans.txt")
        fig_id = "chord_" + str(idx)
        content = db_lt.DrawCompareFigure(dirName, fig_id, figure)
        cards.append(content)
        idx = idx + 1

    layout = html.Div(
        dbc.CardGroup(
            cards
        )
    )

    return layout

def GetHeatmapPicture(path):
    suitableFilename = []
    for root, dirs, files in os.walk(path, topdown=False):
        for name in files:
            # .meta is for unity directory, .meta not in exe direcrory
            if (".meta" not in name) and (("HeatMap" in name) and ((".png" in name))):
                filename = name.replace(".png", '')
                suitableFilename.append(filename)
    # get biggest index move/stay heatmap (default)
    moveIndex = 0
    stayIndex = 0
    for filename in suitableFilename:
        split = filename.split('_')
        if split[0] == "moveHeatMap" and int(split[1]) > moveIndex: moveIndex = int(split[1])
        if split[0] == "stayHeatMap" and int(split[1]) > stayIndex: stayIndex = int(split[1])
    
    moveHeatmapPic = GetPicture(path + "moveHeatMap_" + str(moveIndex) + ".png")
    stayHeatmapPic = GetPicture(path +"stayHeatMap_" + str(stayIndex) + ".png")

    return moveHeatmapPic, stayHeatmapPic
    
def CompareHeatmap():
    moveCards = []
    stayCards = []
    idx = 1
    for path , dirName in zip(pathList, dirNameList):
        moveFigure, stayFigure = GetHeatmapPicture(path)
        moveFig_id = "move_" + str(idx)
        stayFig_id = "stay_" + str(idx)
        content = db_lt.DrawCompareFigure(dirName, moveFig_id, moveFigure)
        moveCards.append(content)
        content = db_lt.DrawCompareFigure(dirName, stayFig_id, stayFigure)
        stayCards.append(content)
        idx = idx + 1

    moveLayout = html.Div(
        dbc.CardGroup(
            moveCards
        )
    )

    stayLayout = html.Div(
        dbc.CardGroup(
            stayCards
        )
    )

    return moveLayout, stayLayout

def GetVisitorCountInEx(filename):
    df = pd.read_csv(filename, sep = ' ', header = None)
    df = df.iloc[: , :-1] # remove the last data (NaN)
    df = df.T
    columnNames = []
    [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns))]
    df.columns = columnNames
    fig = px.line(df, title = "Real-time visitor number in the exhibit")
    fig.update_layout(xaxis_title = "time (sec)", yaxis_title = "number of visitor", legend = dict(title = "exhibit"))
    fig = SetFigureTemplate(fig)
    return fig

def CompareVisitorCountInEx():
    cards = []
    idx = 1
    for path , dirName in zip(pathList, dirNameList):
        figure = GetVisitorCountInEx(path + "ex_realtime_human_count.txt")
        fig_id = "visitorCount_" + str(idx)
        content = db_lt.DrawCompareFigure(dirName, fig_id, figure)
        cards.append(content)
        idx = idx + 1

    layout = html.Div(
        cards
    )
    return layout

def GetStatusDataFrame(filename, dirName):
    df = pd.read_csv(filename, sep = ' ', header = None)
    df.columns = ["status", "time", "id", "humantype"]
    d = []
    [d.append(dirName) for i in range(len(df.index))]
    df["dirName"] = d  
    return df

def CompareStatus():
    dfList = []
    cards = []
    for path , dirName in zip(pathList, dirNameList):
        df = GetStatusDataFrame(path + "status_time.txt", dirName)
        dfList.append(df)

    df = dfList[0]
    for i in range(1, len(dfList)):
        df = pd.concat([df, dfList[i]], ignore_index = True)

    figure = px.box(df, x = "status", y = "time", color = "dirName", points = "outliers")
    figure.update_layout(yaxis_title = "time (sec)")
    figure = SetFigureTemplate(figure)
    
    fig_id = "status"
    content = db_lt.DrawCompareFigure("Status Comparison", fig_id, figure)
    cards.append(content)

    layout = html.Div(
        cards
    )
    return layout

def GetVisitingTimeDataFrame(filename, dirName):
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
            add_df = pd.DataFrame({'exhibition': exhibitionName, 'time': df.loc[j, exhibitionName], 
                                    'id': df.loc[j, 'id'], 'humantype': df.loc[j, 'humantype']}, index = [0])
            new_df = pd.concat([new_df, add_df], ignore_index = True)

    d = []
    [d.append(dirName) for i in range(len(new_df.index))]
    new_df["dirName"] = d
    return new_df

def CompareVisitingTime():
    dfList = []
    cards = []
    for path , dirName in zip(pathList, dirNameList):
        df = GetVisitingTimeDataFrame(path + "visiting_time.txt", dirName)
        dfList.append(df)
    df = dfList[0]
    for i in range(1, len(dfList)):
        df = pd.concat([df, dfList[i]], ignore_index = True)
    
    figure = px.box(df, x = "exhibition", y = "time", color = "dirName", points = "outliers")
    figure = SetFigureTemplate(figure)
    
    fig_id = "visitingTime"
    content = db_lt.DrawCompareFigure("Visiting Time Comparison", fig_id, figure)
    cards.append(content)

    layout = html.Div(
        cards
    )
    return layout

def GetSatisfictionDataFrame(filename, dirName):
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

def CompareSatisfiction():
    dfList = []
    cards = []
    for path , dirName in zip(pathList, dirNameList):
        df = GetSatisfictionDataFrame(path + "satisfiction.txt", dirName)
        dfList.append(df)
    """
    darkerColorList = []
    
    for c in color:
        darkerColorList.append(color_variant(c))
    """
    color = random.sample(colorList, len(dfList))
    figure = go.Figure()
    for i in range(len(dfList)):
        df = dfList[i]
        figure.add_traces([
            go.Bar(x = df.index, y = df["count"], marker_color = color[i], yaxis='y1', name = dirNameList[i]),
            go.Scatter(x = df.index, y = df['ratio'], marker_color = color[i], yaxis='y2', name = dirNameList[i] + ' ratio', hovertemplate = '%{y:.1%}')
        ])

    figure.update_layout(
        template='plotly_dark',
        plot_bgcolor= 'rgba(0, 0, 0, 0)',
        paper_bgcolor= 'rgba(0, 0, 0, 0)',
        margin={
            "pad": 0,
            "t": 0,
            "r": 10,
            "l": 10,
            "b": 0,
        },
        showlegend=True, 
        hovermode='x', 
        bargap=.3,
        title= {'text': 'visitors\' satisfiction', 'x': .5}, 
        yaxis= {'title': 'count'},
        yaxis2={'showgrid': False,
                'rangemode': "tozero", 
                'overlaying': 'y',
                'position': 1, 'side': 'right',
                'title': 'ratio',
                'tickvals': np.arange(0, 1.1, .2),
                'tickmode': 'array',
                'ticktext': [str(i) + '%' for i in range(0, 101, 20)]}
    )

    fig_id = "satisfiction"
    content = db_lt.DrawCompareFigure("Satisfiction Comparison", fig_id, figure)
    cards.append(content)

    layout = html.Div(
        cards
    )
    return layout

def color_variant(hex_color, brightness_offset=1):
    """ takes a color like #87c95f and produces a lighter or darker variant """
    if len(hex_color) != 7:
        raise Exception("Passed %s into color_variant(), needs to be in #87c95f format." % hex_color)
    rgb_hex = [hex_color[x:x+2] for x in [1, 3, 5]]
    new_rgb_int = [int(hex_value, 16) + brightness_offset for hex_value in rgb_hex]
    new_rgb_int = [min([255, max([0, i])]) for i in new_rgb_int] # make sure new values are between 0 and 255
    # hex() produces "0x88", we want just "88"
    return "#" + "".join([hex(i)[2:] for i in new_rgb_int])

#endregion
