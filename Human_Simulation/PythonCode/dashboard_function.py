# -*- coding: utf-8 -*-
"""
dahboard.py functions
@author: chhung
"""
import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import numpy as np
import os
from PIL import Image
import chord_diagram as cd

path = ''

#%% set figure template
def SetFigureTemplate(fig):
    fig.update_layout(template='plotly_dark',
                      plot_bgcolor= 'rgba(0, 0, 0, 0)',
                      paper_bgcolor= 'rgba(0, 0, 0, 0)',
                      margin={
                          "pad": 0,
                          "t": 0,
                          "r": 10,
                          "l": 10,
                          "b": 0,
                      },
                     )
    return fig

#for heat map
def HideXYAxisTicks(fig, width, height):
    fig.update_xaxes(showticklabels = False)
    fig.update_yaxes(showticklabels = False)
    #fig.update_layout(width = width, height = height)
    fig.update_layout(autosize=True)
    return fig

#%% get move/stay heatmap png & layout file in the directory
def PngToPlotlyFigure(filePath, width, height):
    img = np.array(Image.open(filePath))
    fig = px.imshow(img)
    fig = SetFigureTemplate(fig)
    fig = HideXYAxisTicks(fig, width, height)
    return fig

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
    moveHeatMap = PngToPlotlyFigure(path + "moveHeatMap_" + str(moveIndex) + ".png", 1200, 800)
    stayHeatMap = PngToPlotlyFigure(path +"stayHeatMap_" + str(stayIndex) + ".png", 1200, 800)
    layout = PngToPlotlyFigure(path + "layout_screenshot.png", 600, 600)


    return moveHeatMap, stayHeatMap, layout

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
    fig.update_layout(yaxis_title = "time (sec)")
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
    fig.update_layout(xaxis_title = "time (sec)", yaxis_title = "number of visitor", legend = dict(title = "exhibit"))
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
    maxTime = df["time"].max()
    ylabelArray = []
    ylabelText = []
    num = 0
    while( num < maxTime):
        ylabelArray.append(num)
        ylabelText.append(str(num))
        num = num + 10
    ylabelArray.append(num)
    ylabelText.append(str(num))
    #fig.update_layout(yaxis = dict(tickmode = "linear", tick0 = 0, dtick = 20))
    fig.update_layout(yaxis = dict(tickmode = "array", tickvals = ylabelArray, ticktext = ylabelText), yaxis_title = "time (sec)")
    fig = SetFigureTemplate(fig)
    return fig

#%% visitor satisfiction pareto_chart
def GetFigure_VisitorSatisfiction():
    file = path + "satisfiction.txt"
    try:
        with open(file, 'r') as f:
            rawData = f.readlines()
            data = []
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
            print(df)
            '''
            collection = pd.Series(data)
            df = collection.value_counts().to_frame('counts').join(collection.value_counts(normalize=True).cumsum().to_frame('ratio'))
            '''

            # plot fig
            fig = go.Figure([go.Bar(x=df.index, y=df['count'], yaxis='y1', name='visitor count'),
                             go.Scatter(x=df.index, y=df['ratio'], yaxis='y2', name='cumulative ratio',
                                        hovertemplate='%{y:.1%}', marker={'color': '#000000'})])

            fig.update_layout(template='plotly_dark', showlegend=True, hovermode='x', bargap=.3,
                      plot_bgcolor= 'rgba(0, 0, 0, 0)',
                      paper_bgcolor= 'rgba(0, 0, 0, 0)',
                      xaxis_title="satisfiction percentage",
                      title= {'text': 'visitors\' satisfiction', 'x': .5}, 
                      yaxis= {'title': 'visitor count'},
                      yaxis2={'showgrid': False,
                              'rangemode': "tozero", 
                              'overlaying': 'y',
                              'position': 1, 'side': 'right',
                              'title': 'ratio',
                              'tickvals': np.arange(0, 1.1, .2),
                              'tickmode': 'array',
                              'ticktext': [str(i) + '%' for i in range(0, 101, 20)]},
                      legend= {'orientation': "h", 'yanchor': "bottom", 'y': 1.02, 'xanchor': "right", 'x': 1})
            return fig
    except Exception as ex:
        template = "An exception of type {0} occurred. Arguments:\n{1!r}"
        message = template.format(type(ex).__name__, ex.args)
        print(message)
        # return template fig
        fig = dict({"data": [{"type": "bar",
                              "x": [1, 2, 3],
                              "y": [1, 3, 2]}],
                    "layout": {"title": {"text": "A Figure Specified By Python Dictionary"}}
                })
        return fig


#%% convert dash content to html
def ConvertDashToHtml(filePath, fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount, \
                      fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction, descriptions, PDF = False):
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
        FigureToHtml(f, "展品佈局圖", "", fig_layout, 'cdn')
        FigureToHtml(f, "轉移機率", descriptions[5], fig_chord, False)
        FigureToHtml(f, "展廳移動熱區", descriptions[0], fig_moveHeatMap, False)
        FigureToHtml(f, "展廳停留熱區", descriptions[1], fig_stayHeatMap, False)
        FigureToHtml(f, "展品及時人數", descriptions[2], fig_exhibitionRealtimeVisitorCount, False)
        FigureToHtml(f, "遊客移動狀態", descriptions[3], fig_visitorStatusTime, False)
        FigureToHtml(f, "遊客觀看時間", descriptions[4], fig_visitorVisitingTimeInEachExhibit, False)
        FigureToHtml(f, "遊客觀看時間", descriptions[6], fig_visitorSatisfiction, False)
        
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



































