# -*- mode: python ; coding: utf-8 -*-
# This is a specification file for compiling the application into a single folder

block_cipher = None


a = Analysis(['newDashboard.py'],
             pathex=['D:\\ChengHao\\thesisCode\\Human_Simulation\\PythonCode'],
             binaries=[],
             datas=[
                  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/dash_core_components', 'dash_core_components'),
                  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/dash_html_components', 'dash_html_components'),
                  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/dash_bootstrap_components', 'dash_bootstrap_components'),
                  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/plotly', 'plotly'),
                  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/dash_renderer', 'dash_renderer'),
                  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/dash', 'dash'),
				  ('C:/Users/chhung/anaconda3/envs/myenv/Lib/site-packages/colorlover', 'colorlover'),
				  ('D:/ChengHao/thesisCode/Human_Simulation/PythonCode/data', 'data'),
				  ('D:/ChengHao/thesisCode/Human_Simulation/PythonCode/pages', 'pages')
                  ],
             hiddenimports=[],
             hookspath=[],
             runtime_hooks=[],
             excludes=[],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher,
             noarchive=False)
pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(pyz,
          a.scripts,
          [],
          exclude_binaries=True,
          name='dashboard',
          debug=False,
          bootloader_ignore_signals=False,
          strip=False,
          icon = '',
          upx=True,
          console=True )
coll = COLLECT(exe,
               a.binaries,
               a.zipfiles,
               a.datas,
               strip=False,
               upx=True,
               upx_exclude=[],
               name='dashboard_exe')