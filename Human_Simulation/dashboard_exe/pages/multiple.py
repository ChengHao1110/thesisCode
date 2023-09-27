"""
multiple simulation result page
@author: chhung
"""

import dash
from dash import Dash, dcc, html, Input, Output, State, MATCH, ALL
import dash_bootstrap_components as dbc

import os

import dashboard_layout as db_lt
import compare_function as cmp_fun

dash.register_page(__name__)

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
            allDirectory.append(dirName)
    return simulationTopPath, allDirectory

def GetCompareFigures(pathList):
    cmp_fun.pathList = pathList

#endregion

#region parameters
# simulation result directories
simulationTopDirectory = ''
allDirectory = []

# compare path list & dirName list
compPathList = []
dirNameList = []

# compare figures
fig_moveHeatmapList = []
fig_moveStaymapList = []
fig_layoutList = []
fig_exVisCountList = []
fig_chordList =[]
fig_compVisStatus = ''
fig_compVisVisitingTime = ''
fig_compVisSatisfiction = ''

# dynamic layout
comparePart = []

#endregion

#region initialize parameters
simulationTopDirectory, allDirectory = GetSimulationResultDirectories()
allDirectory = ["None"] + allDirectory
#print(allDirectory)
#endregion

layout = html.Div([
    dbc.Container([
        #title
        dbc.Row(
            dbc.Col([
                html.H1("Multiple Simulation Result Analysis"),
                html.Br()
            ])
        ),
        dbc.Row([
            # compare
            dbc.Col([
                dbc.Card(
                    dbc.CardBody([
                        # card title
                        dbc.Row(
                            html.H3("Select directories")
                        ),
                        dbc.Row([
                            dbc.ButtonGroup([
                                dbc.Button("ADD (+)", id = "add_dropdown", n_clicks = 0, color = "success", className="me-1"),
                                dbc.Button("REMOVE (-)", id = "remove_dropdown", n_clicks = 0, color = "danger", className="me-1")
                                ],
                                size = "lg",
                                className = "me-1"
                            )
                        ]),
                        html.Hr(),
                        html.Div(id='dropdown_container', children = []),
                        html.Div(id = 'empty_container'),
                        html.Hr(),
                        html.Div([
                            dbc.Button("Comparison", id = "compare_button", n_clicks = 0, color = "info", className = "me-1", size = "lg")
                            ],
                            className="d-grid mx-auto"
                        )

                    ]
                    ) # body
                ) # card
            ], width = 3),
            # figures
            dbc.Col([
                # layout figures
                html.H3(html.H3("展品布局圖")),
                html.Div(id = 'layout_container'),
                html.Hr(),
                html.H3("轉移機率圖"),
                html.Div(id = 'chord_container'),
                html.Hr(),
                html.H3("移動熱區圖"),
                html.Div(id = 'moveHeatmap_container'),
                html.Hr(),
                html.H3("停留熱區圖"),
                html.Div(id = 'stayHeatmap_container'),
                html.Hr(),
                html.H3("展品即時人數統計圖"),
                html.Div(id = 'visitorCount_container'),
                html.Hr(),
                html.H3("移動狀態"),
                html.Div(id = 'status_container'),
                html.Hr(),
                html.H3("觀看時間"),
                html.Div(id = 'visitingTime_container'),
                html.Hr(),
                html.H3("滿意度"),
                html.Div(id = 'satisfiction_container'),
                html.Hr(),                
            ], width = 9)
        ])
    ], fluid = True) # container end
   
])

# input field
@dash.callback(
    Output('dropdown_container', 'children'),
    Input('add_dropdown', 'n_clicks'),
    Input('remove_dropdown', 'n_clicks'),
    State('dropdown_container', 'children')
)
def SetDropdownList(add_clicks, remove_clicks, div_children):
    ctx = dash.callback_context
    triggered_id = ctx.triggered[0]['prop_id'].split('.')[0]
    if triggered_id == 'add_dropdown':
        new_children = html.Div(
            children = [
                dcc.Dropdown(
                    options = allDirectory, 
                    value = allDirectory[0], 
                    clearable = True, 
                    id = {
                        'type': 'compare_dropdown',
                        'index': add_clicks
                    }
                ),
                html.Br()
            ]
        )
        div_children.append(new_children)

    elif triggered_id == 'remove_dropdown' and len(div_children) > 0:
        div_children = div_children[:-1]

    return div_children

# set compare path list by dropdown
@dash.callback(
    Output('empty_container', 'children'),
    Input({'type': 'compare_dropdown', 'index': ALL}, 'value'),
)
def SetComparePathList(values):
    global compPathList, dirNameList
    compPathList = []
    dirNameList = []
    for (i, value) in enumerate(values):
        compPathList.append(simulationTopDirectory + value + '\\')
        dirNameList.append(value)
    #print(compPathList)

# plot compare figure
@dash.callback(
    Output('layout_container', 'children'),
    Output('chord_container', 'children'),
    Output('moveHeatmap_container', 'children'),
    Output('stayHeatmap_container', 'children'),
    Output('visitorCount_container', 'children'),
    Output('status_container', 'children'),
    Output('visitingTime_container', 'children'),
    Output('satisfiction_container', 'children'),
    Input('compare_button', 'n_clicks'),
)
def Compare(n_clicks):
    if(len(compPathList) > 0 and n_clicks >= 1):
        cmp_fun.pathList = compPathList
        cmp_fun.dirNameList = dirNameList
        layout = cmp_fun.CompareLayout()
        chord = cmp_fun.CompareChordDiagram()
        move, stay = cmp_fun.CompareHeatmap()
        visCount  = cmp_fun.CompareVisitorCountInEx()
        status = cmp_fun.CompareStatus()
        visitingTime = cmp_fun.CompareVisitingTime()
        satisfiction = cmp_fun.CompareSatisfiction()
        return layout, chord, move, stay, visCount, status, visitingTime, satisfiction