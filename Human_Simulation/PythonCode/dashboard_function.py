# -*- coding: utf-8 -*-
"""
dahboard.py functions
@author: chhung
"""
import pandas as pd
import plotly.express as px
import numpy as np
import os
from PIL import Image
import chord_diagram as cd

path = ''

#%% set figure template
def SetFigureTemplate(fig):
    fig.update_layout(template='plotly_dark',
                      plot_bgcolor= 'rgba(0, 0, 0, 0)',
                      paper_bgcolor= 'rgba(0, 0, 0, 0)')
    return fig

#for heat map
def HideXYAxisTicks(fig):
    fig.update_xaxes(showticklabels = False)
    fig.update_yaxes(showticklabels = False)
    fig.update_layout(width = 600, height = 600)
    return fig

#%% get move/stay heatmap png file in the directory
def GetFigure_HeapMap():
    # get suitable heat map files
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
    '''
    # check get right figure
    print("moveHeatMap_" + str(moveIndex) + ".png")
    print("stayHeatMap_" + str(stayIndex) + ".png")
    '''
    
    moveFilename = path + "moveHeatMap_" + str(moveIndex) + ".png"
    stayFilename = path +"stayHeatMap_" + str(stayIndex) + ".png"
    img = np.array(Image.open(moveFilename))
    moveHeatMap = px.imshow(img)
    moveHeatMap = SetFigureTemplate(moveHeatMap)
    moveHeatMap = HideXYAxisTicks(moveHeatMap)
    img = np.array(Image.open(stayFilename))
    stayHeatMap = px.imshow(img)
    stayHeatMap = SetFigureTemplate(stayHeatMap)
    stayHeatMap = HideXYAxisTicks(stayHeatMap)
    return moveHeatMap, stayHeatMap

#%% get visitor visiting time  
def GetFigure_VistorVisitingTimeInEachExhibit():
    file = path + "visiting_time.txt"
    df = pd.read_csv(file, sep = ' ', header = None)
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

    fig = px.box(new_df, x="exhibition", y="time", color = "humantype", points = "all")
    fig = SetFigureTemplate(fig)
    return fig

#%% exhibition realtime visitor count
def GetFigure_ExhibitionRealtimeVisitorCount():
    file = path + "ex_realtime_human_count.txt"
    df = pd.read_csv(file, sep = ' ', header = None)
    df = df.iloc[: , :-1] # remove the last data (NaN)
    df = df.T
    columnNames = []
    [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns))]
    df.columns = columnNames
    fig = px.line(df, title = "Real-time visitor number in the exhibit")
    fig.update_layout(xaxis_title = "time", yaxis_title = "number of visitor")
    fig = SetFigureTemplate(fig)
    return fig

#%% chord diagram
def GetFigure_ChordDiagram():
    file = path + "ex_trans.txt"
    df = pd.read_csv(file, sep = ' ', header = None)
    df = df.iloc[: , :-1] # remove the last data (NaN)
    df = df.T
    columnNames = []
    [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns) - 2)] #remove 2 exit
    columnNames.append('exit1')
    columnNames.append('exit2')
    df.columns = columnNames
    
    fig = cd.make_filled_chord(df)
    return fig

#%% visitor status time 
def GetFigure_VisitorStatusTime():
    file = path + "status_time.txt"
    df = pd.read_csv(file, sep = ' ', header = None)
    df.columns = ["status", "time", "id", "humantype"]
    fig = px.box(df, x = "status", y = "time", color = "humantype", points = "all")
    fig = SetFigureTemplate(fig)
    return fig

#%% convert dash content to html
def ConvertDashToHtml(filePath, fig_moveHeatMap, fig_stayHeatMap, fig_exhibitionRealtimeVisitorCount, \
                      fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, descriptions, PDF = False):
    frac = filePath.split('\\')
    dirName = frac[-2]
    if PDF:
        filename = filePath + "pdf.html"
    else:
        filename = filePath + dirName + '.html'
    with open(filename, 'w') as f:
        htmlStart, htmlEnd = HtmlSetUp()
        f.write(htmlStart)
        f.write("<div> <h1>Analysis Data DashBoard: " + dirName +"</h1> </div>")
        FigureToHtml(f, "展廳移動熱區", descriptions[0], fig_moveHeatMap, 'cdn')
        FigureToHtml(f, "展廳停留熱區", descriptions[1], fig_stayHeatMap, False)
        FigureToHtml(f, "展品及時人數", descriptions[2], fig_exhibitionRealtimeVisitorCount, False)
        FigureToHtml(f, "遊客移動狀態", descriptions[3], fig_visitorStatusTime, False)
        FigureToHtml(f, "遊客觀看時間", descriptions[4], fig_visitorVisitingTimeInEachExhibit, False)
        FigureToHtml(f, "轉移機率", descriptions[5], fig_chord, False)
        f.write(htmlEnd)
        f.close()
        
def FigureToHtml(file, title, description, figure, include_plotlyjs):
    file.write("<div> <h3>" + title +"</h3> </div>")
    file.write("<div> <p>" + description +"</p> </div>")
    file.write(figure.to_html(full_html=False, include_plotlyjs= include_plotlyjs))
    
def HtmlSetUp():
    with open('data/htmlFormat.txt', 'r') as f:
        content = f.read()
        frac = content.split('+')
        return frac[0], frac[1]



































