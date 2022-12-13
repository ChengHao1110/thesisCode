# -*- coding: utf-8 -*-
"""
use plotly & dash to build a data visualization dashboard

@author: chhung
"""
import dashboard_function as db_fnc
import dashboard_layout as db_lt

from dash import Dash, dcc, html, Input, Output
import dash_bootstrap_components as dbc

import os
import webbrowser

#%% get file execute path
myPath = ''
#print(os.path.dirname(os.path.realpath(__file__)))
for root, dirs, files in os.walk('..'):
    for dirName in dirs:
        if dirName == "Simulation_Result":
            myPath = os.path.join(root, dirName)
myPath = os.path.abspath(myPath)

#%% get all simulation result directory
#simFileDirectory = "D:/ChengHao/thesisCode/Human_Simulation/Assets/StreamingAssets/Simulation_Result/"
#db_fnc.path = "D:/ChengHao/thesisCode/Human_Simulation/Assets/StreamingAssets/Simulation_Result/sample3/"
simFileDirectory = myPath + '\\'
allDirectory = []
for dirName in os.listdir(simFileDirectory):
    # .meta is for unity directory
    if ".meta" not in dirName:
        allDirectory.append(simFileDirectory + dirName + '\\')

#%% get all figures in the selected directory
fig_moveHeatMap = ''
fig_stayHeatMap = ''
fig_layout = ''
fig_exhibitionRealtimeVisitorCount = ''
fig_visitorStatusTime = ''
fig_visitorVisitingTimeInEachExhibit = '' 
fig_chord = ''
fig_visitorSatisfiction = ''

def GetFigures(path):
    print(path)
    db_fnc.path = path
    fig_moveHeatMap, fig_stayHeatMap, fig_layout = db_fnc.GetFigure_HeapMap()
    fig_visitorVisitingTimeInEachExhibit = db_fnc.GetFigure_VistorVisitingTimeInEachExhibit()
    fig_exhibitionRealtimeVisitorCount = db_fnc.GetFigure_ExhibitionRealtimeVisitorCount()
    fig_chord = db_fnc.GetFigure_ChordDiagram()
    fig_visitorStatusTime = db_fnc.GetFigure_VisitorStatusTime()
    fig_visitorSatisfiction = db_fnc.GetFigure_VisitorSatisfiction()
    return fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount, \
    fig_visitorStatusTime ,fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction

fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount, \
fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction = GetFigures(allDirectory[0])

#%% get all description
# 0: 展廳移動熱區
# 1: 展廳停留熱區
# 2: 展品及時人數
# 3: 遊客移動狀態
# 4: 遊客觀看時間
# 5: 轉移機率
# 6: 遊客滿意度
with open('data/figure_description.txt', 'r', encoding='utf-8') as f:
    descriptions = f.readlines()

#%% dashboard layout
#app layout style
#app = Dash(__name__, external_stylesheets=[dbc.themes.BOOTSTRAP])
app = Dash(__name__, external_stylesheets=[dbc.themes.SLATE])
server = app.server
# use dbc to build the app layout
app.layout = dbc.Container([
    dbc.Card(
        dbc.CardBody([
            # title
            dbc.Row([
                dbc.Col([
                    html.H1("Analysis Data Dashboard"),
                    html.Br()
                ])
            ]), #title end
            
            # Browse Text
            dbc.Row([
                dbc.Col([
                    html.H5("Browse Simulation Result Directory : "),
                    html.Br()
                ], width = 4)
            ], justify = "start", align = "center"), #dropdown end

            # path Dropdown
            dbc.Row([
                dbc.Col([
                    dcc.Dropdown(options = allDirectory, value = allDirectory[0], clearable = True, id = 'selected_path'),
                    html.Br()
                ], width = 12)
            ],justify = "start", align = "center"),
            
            # figures

            # 1 Row start
            dbc.Row([
                # layout figure
                dbc.Col([
                    db_lt.DrawFigure(title = "展品佈局圖",
                                     description = "此為展品放置的俯視圖，給其他分析圖參考用",
                                     figId = "layout_figure",
                                     fig = fig_layout)
                ], width = 6), # (1, 1)
                # chord diagram
                dbc.Col([
                    db_lt.DrawFigure(title = "轉移機率",
                                     description = descriptions[5],
                                     figId = "chord_diagram",
                                     fig = fig_chord)
                ], width = 6) # (1, 2)
            ], align = "center"),# 1 Row end

            
            # 2 row start
            #dbc.Row([
            #    # move heat map
            #    dbc.Col([
            #        db_lt.DrawFigure(title = "展廳移動熱區",
            #                         description = descriptions[0],
            #                         figId = "move_heatmap_figure",
            #                         fig = fig_moveHeatMap)
            #    ], width = 6), # (2, 1)
            #    #stay heat map
            #    dbc.Col([
            #        db_lt.DrawFigure(title = "展廳停留熱區",
            #                         description = descriptions[1],
            #                         figId = "stay_heatmap_figure",
            #                         fig = fig_stayHeatMap)
            #    ], width = 6) # (2, 2)
            #], align = "center"), # 2 row end
            

            dbc.Row([
                # move heat map
                dbc.Col([
                    db_lt.DrawFigure(title = "展廳移動熱區",
                                     description = descriptions[0],
                                     figId = "move_heatmap_figure",
                                     fig = fig_moveHeatMap)
                ]) # (2, 1)
            ]), # 2 row end

            dbc.Row([
                #stay heat map
                dbc.Col([
                    db_lt.DrawFigure(title = "展廳停留熱區",
                                     description = descriptions[1],
                                     figId = "stay_heatmap_figure",
                                     fig = fig_stayHeatMap)
                ]) # (2, 2)
            ]), # 2 row end

            
            # 3 row start
            dbc.Row([
                # real time visitor count in each exhibit
                dbc.Col([
                    db_lt.DrawFigure(title = "展品及時人數",
                                     description = descriptions[2],
                                     figId = "realtime_visitor_count",
                                     fig = fig_exhibitionRealtimeVisitorCount)
                ]) # (3, 1)
            ]), # 3 Row end
            
            # 4 row start
            dbc.Row([
                # visitor watch time in each exhibit
                dbc.Col([
                    db_lt.DrawFigure(title = "遊客觀看時間",
                                     description = descriptions[4],
                                     figId = "visitor_visit_time",
                                     fig = fig_visitorVisitingTimeInEachExhibit)
                ]) # (4, 1)
            ]), # 4 Row end

            # 5 row start
            dbc.Row([
                dbc.Col([
                    #visitor satisfiction
                     db_lt.DrawFigure(title = "遊客滿意度",
                                     description = descriptions[6],
                                     figId = "visitor_satisfiction",
                                     fig = fig_visitorSatisfiction)
                ])
            ]), # 5 row end

            # 6 row start
            dbc.Row([
                # visitor status time
                dbc.Col([
                    db_lt.DrawFigure(title = "遊客移動狀態",
                                     description = descriptions[3],
                                     figId = "visitor_status",
                                     fig = fig_visitorStatusTime)
                ], width = 6) # (5, 1)
            ]),
            
            # buttons
            dbc.Row([
                dbc.Col([
                    dbc.Button("Export to html file", id = "html_button", n_clicks = 0, className = "me-1")
                ], width = 2),
                dbc.Col([
                    html.H3(children = '',id = "download_msg")
                ], width = 10)
            ], align = "center", justify = "start")# buttons end
        ]) # CardBody
    ) # Card
]) # Container

#%% app callback
times = 1
@app.callback(
    Output(component_id = 'move_heatmap_figure', component_property ='figure'),
    Output(component_id = 'stay_heatmap_figure', component_property ='figure'),
    Output(component_id = 'layout_figure', component_property ='figure'),
    Output(component_id = 'realtime_visitor_count', component_property ='figure'),
    Output(component_id = 'visitor_status', component_property ='figure'),
    Output(component_id = 'visitor_visit_time', component_property ='figure'),
    Output(component_id = 'chord_diagram', component_property ='figure'),
    Output(component_id = 'visitor_satisfiction', component_property ='figure'),
    Output(component_id = 'download_msg', component_property ='children'),
    Input(component_id = 'selected_path', component_property = 'value'),
    Input(component_id = 'html_button', component_property = 'n_clicks')
)
def UpdatePath(selected_path, html_clicks):
    global times
    msg = ""
    fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount, \
    fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction = GetFigures(selected_path)
    if html_clicks >= times:
        times = times + 1
        db_fnc.ConvertDashToHtml(selected_path, fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount \
                                 , fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction, descriptions, False)
        filename = (selected_path.split('\\'))[-2]
        msg = "The {filename}.html was downloaded in the directory!".format(filename = filename)
    return fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount, \
           fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction, msg

if __name__ == '__main__':
    webbrowser.open_new('http://127.0.0.1:8049/')
    app.server.run(debug = False, host = '127.0.0.1', port = 8049)
    #app.run_server(debug=False)





