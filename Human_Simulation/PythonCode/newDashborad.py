"""
dashboard for simulation result
single result & compare multi page
@author: chhung
"""

import dash
from dash import html, dcc
import dash_bootstrap_components as dbc

# official lib
import webbrowser

# import my additional function
# build exe
import chord_diagram
import dashboard_function
import dashboard_layout


app = dash.Dash(__name__, external_stylesheets=[dbc.themes.SLATE], use_pages=True)

app.layout = html.Div(
    [
        # main app framework
        html.Div("Simulation Result Dashboard", style={'fontSize':50, 'textAlign':'center'}),
        html.Div([
            dcc.Link(page['name']+"  |  ", href=page['path'])
            for page in dash.page_registry.values()
        ],
        style = {
            'fontSize': 20,
            'margin-left': '2%'
        }
        ),
        html.Hr(),

        # content of each page
        dash.page_container
    ]
)


if __name__ == "__main__":
    webbrowser.open_new('http://127.0.0.1:8049/')
    app.server.run(debug = False, host = '127.0.0.1', port = 8049)