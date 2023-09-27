"""
single simulation result page
@author: chhung
"""

import dash
from dash import Dash, dcc, html, Input, Output
import dash_bootstrap_components as dbc

import os

import dashboard_function as db_fnc
import dashboard_layout as db_lt

dash.register_page(__name__, path='/') # home page

#region function definition
def GetSimulationResultDirectories():
    simulationTopPath = ''
    for root, dirs, files in os.walk('..'):
        for dirName in dirs:
            if dirName == "Simulation_Result":
                simulationTopPath = os.path.join(root, dirName)
    simulationTopPath = os.path.abspath(simulationTopPath) + '\\'
    allDirectory = []
    for dirName in os.listdir(simulationTopPath):
        # .meta is for unity directory
        if ".meta" not in dirName:
            allDirectory.append(simulationTopPath + dirName + '\\')
    return allDirectory

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

def GetFigureDescriptions():
    # 0: 展廳移動熱區
    # 1: 展廳停留熱區
    # 2: 展品及時人數
    # 3: 遊客移動狀態
    # 4: 遊客觀看時間
    # 5: 轉移機率
    # 6: 遊客滿意度
    with open('data/figure_description.txt', 'r', encoding='utf-8') as f:
        descriptions = f.readlines()
        return descriptions
#endregion

#region parameters
# simulation result directories
allDirectory = []

# figures
fig_moveHeatMap = ''
fig_stayHeatMap = ''
fig_layout = ''
fig_exhibitionRealtimeVisitorCount = ''
fig_visitorStatusTime = ''
fig_visitorVisitingTimeInEachExhibit = '' 
fig_chord = ''
fig_visitorSatisfiction = ''

# figure description
descriptions = []

# callback
times = 1 # button
#endregion

#region initialize parameters
allDirectory = GetSimulationResultDirectories()
fig_moveHeatMap, fig_stayHeatMap, fig_layout, fig_exhibitionRealtimeVisitorCount, \
fig_visitorStatusTime, fig_visitorVisitingTimeInEachExhibit, fig_chord, fig_visitorSatisfiction = GetFigures(allDirectory[0])
descriptions = GetFigureDescriptions()
#endregion

#region single page layout
layout = html.Div([
    dbc.Container([
        # title
        dbc.Row([
            dbc.Col([
                html.H1("Single Simulation Result Analysis"),
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
        html.Hr(),

        # 1 Row start
        dbc.Row([
            dbc.CardGroup([
                # layout
                db_lt.DrawFigure(title = "展品佈局圖",
                                description = "此為展品放置的俯視圖，給其他分析圖參考用",
                                figId = "layout_figure",
                                fig = fig_layout),
                # chord diagram
                db_lt.DrawFigure(title = "轉移機率",
                                description = descriptions[5],
                                figId = "chord_diagram",
                                fig = fig_chord)
            ])
        ]),
        html.Hr(),

        # 2 row start
        dbc.Row([
            dbc.CardGroup([
                # move heat map
                db_lt.DrawFigure(title = "展廳移動熱區",
                                 description = descriptions[0],
                                 figId = "move_heatmap_figure",
                                 fig = fig_moveHeatMap),
                #stay heat map
                db_lt.DrawFigure(title = "展廳停留熱區",
                                 description = descriptions[1],
                                 figId = "stay_heatmap_figure",
                                 fig = fig_stayHeatMap)
            ]) # (2, 1)
        ]), # 2 row end
        html.Hr(),

        # 4 row start
        dbc.Row([
            # real time visitor count in each exhibit
            dbc.Col([
                db_lt.DrawFigure(title = "展品及時人數",
                                    description = descriptions[2],
                                    figId = "realtime_visitor_count",
                                    fig = fig_exhibitionRealtimeVisitorCount)
            ]) # (3, 1)
        ]), # 4 Row end
        html.Hr(),
        
        # 5 row start
        dbc.Row([
            # visitor watch time in each exhibit
            dbc.Col([
                db_lt.DrawFigure(title = "遊客觀看時間",
                                    description = descriptions[4],
                                    figId = "visitor_visit_time",
                                    fig = fig_visitorVisitingTimeInEachExhibit)
            ]) # (4, 1)
        ]), # 5 Row end
        html.Hr(),

        # 6 row start
        dbc.Row([
            dbc.Col([
                #visitor satisfiction
                    db_lt.DrawFigure(title = "遊客滿意度",
                                    description = descriptions[6],
                                    figId = "visitor_satisfiction",
                                    fig = fig_visitorSatisfiction)
            ])
        ]), # 6 row end
        html.Hr(),

        # 7 row start
        dbc.Row([
            # visitor status time
            dbc.Col([
                db_lt.DrawFigure(title = "遊客移動狀態",
                                    description = descriptions[3],
                                    figId = "visitor_status",
                                    fig = fig_visitorStatusTime)
            ], width = 6) # (5, 1)
        ]),
        html.Hr(),

            # buttons
        dbc.Row([
            dbc.Col([
                dbc.Button("Export to html file", id = "html_button", n_clicks = 0, className = "me-1")
            ], width = 2),
            dbc.Col([
                html.H3(children = '',id = "download_msg")
            ], width = 10)
        ], align = "center", justify = "start")# buttons end

    ], fluid=True) # container end   
])
#endregion

#region single page callback
@dash.callback(
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
#endregion  

