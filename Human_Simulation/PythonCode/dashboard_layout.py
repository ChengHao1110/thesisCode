# -*- coding: utf-8 -*-
"""
dahboard.py layout
@author: chhung
"""
from dash import dcc, html
import dash_bootstrap_components as dbc

def DrawFigure(title, description, figId, fig):
    return  html.Div([
                dbc.Card(
                    dbc.CardBody([
                        html.H3(title),
                        html.H5(description),
                        html.Div(
                            dcc.Graph(id = figId,
                                      figure = fig,
                                      style = {
                                        "align" : "center"
                                      }
                            )
                        )
                    ])
                ),
                html.Br()
            ])

def WriteText(text):
    return  html.Div([
        dbc.Card(
            dbc.CardBody([
                html.H3(text)
            ])
        )
    ])

