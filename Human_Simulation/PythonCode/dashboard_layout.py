# -*- coding: utf-8 -*-
"""
dahboard.py layout
@author: chhung
"""
from dash import dcc, html
import dash_bootstrap_components as dbc

def DrawFigure(title, description, figId, fig):
    return  dbc.Card(
                dbc.CardBody([
                    html.H3(title, className="card-title"),
                    html.H5(description, className="card-text"),
                    html.Div(
                        dcc.Graph(
                            id = figId,
                            figure = fig,
                        )                 
                    )
                ])
            )

def DrawCompareFigure(title, figId, fig):
    return  dbc.Card(
                dbc.CardBody([
                    html.H3(title, className="card-title", style = {"text-align" : "center"} ),
                    html.Div(
                        dcc.Graph(
                            id = figId,
                            figure = fig,
                        )        
                    )
                ])
            )

def EmptyCard():
    return  dbc.Card(
        dbc.CardBody(
            html.Div()
        )
    )

def CompareFigure3InARow(cards, width):
    layout = dbc.Row(
        [
            dbc.Col(cards[0], width = width),
            dbc.Col(cards[1], width = width),
            dbc.Col(cards[2], width = width),
        ], className = "g-0"),
    return layout

def CompareFigure2InARow(cards, width):
    layout = dbc.Row(
        [
            dbc.Col(cards[0], width = width),
            dbc.Col(cards[1], width = width),
        ], className = "g-0"),
    return layout

def CompareFigure1InARow(cards, width):
    layout = dbc.Row(
        [
            dbc.Col(cards[0], width = width),
        ], className = "g-0"),
    return layout

