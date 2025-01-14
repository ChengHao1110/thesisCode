# -*- coding: utf-8 -*-
"""
Created on Wed Feb 16 00:32:26 2022

@author: Cheng-Hao Hung
"""

import numpy as np
import seaborn as sns
import pandas as pd
import matplotlib as mpl
#mpl.use('Agg')
import matplotlib.pyplot as plt
from PIL import Image
from mpl_chord_diagram import chord_diagram

import os


path = ''
folderList = []
compareResultCount = 1
savePath = ''

# heatmap method
def Usage(filename, dtype):
    fig, axes = plt.subplots(1, compareResultCount, squeeze = False)
    fig.set_facecolor('xkcd:steel')
    
    # read data
    data = []
    # single
    if(compareResultCount == 1):
        fig.suptitle(filename.replace('.txt', ''))
        file = path + folderList[0] + '/' + filename
        data.append(np.loadtxt(file, dtype))
        print(np.max(data[0]))
    # multiple
    else:
        fig.suptitle('Compare ' + filename.replace('.txt', ''), y = .9)
        for i in range(compareResultCount):
            file = path + folderList[i] + '/' + filename;
            data.append(np.loadtxt(file, dtype))



    
    # plot
    for i in range(compareResultCount):
        maxData = np.max(data[i])
        data[i] = data[i] / maxData
        
        axes[0, i].set_title(folderList[i])
        

    #test annot
        file = path + folderList[i] + '/' + 'exhibition_record_usage.txt'
        df = pd.read_csv(file, sep = ' ', header = None)
        #print(df)
        #print(len(df))
        
        t = []
        #print(len(data[i]))
        [t.append("") for j in range(data[i].size)]
        text = np.array(t)
        text = text.reshape(len(data[i]), len(data[i]))
        text = text.tolist()
        
        for j in range(len(df)):
            row = df[0][j]
            col = df[1][j]
            text[row][col] = text[row][col].join('p' + str(j+1))
        
        print(text)

        sns.heatmap(data[i], ax = axes[0, i], square = True, cmap = 'Reds', cbar_kws={"shrink": .5}, annot = text, fmt = '', 
                                linewidths = 1, xticklabels = [], yticklabels = [])
    
    fig.tight_layout()
    if(compareResultCount == 1):
        plt.savefig(savePath + '/' + filename.replace('.txt', ''), dpi=600, bbox_inches='tight', pad_inches=0.02)
    else:
        plt.savefig(savePath + '/Compare ' + filename.replace('.txt', ''), dpi=600, bbox_inches='tight', pad_inches=0.02)
    
    
# exhibition realtime human count
def ExhibitionRealtimeHumanCount(filename):
    fig, axes = plt.subplots(compareResultCount, 1, squeeze = False)
    fig.set_facecolor('xkcd:steel')
    
    #read data
    data = []
    # single
    if(compareResultCount == 1):
        fig.suptitle('Exhibition Realtime Human Count')
        file = path + folderList[0] + '/' + filename
        data.append(np.loadtxt(file, 'int'))
    # multiple
    else:
        fig.suptitle('Compare Exhibition Realtime Human Count')
        for i in range(compareResultCount):
            file = path + folderList[i] + '/' + filename;
            data.append(np.loadtxt(file, 'int'))
    
    #plot
    for i in range(compareResultCount):
        axes[i, 0].set_title(folderList[i])
        totalExhibibiton, totalTime = data[i].shape
        timeline = np.arange(0, totalTime)
        for j in range(totalExhibibiton):
            axes[i, 0].plot(timeline, data[i][j, :], label = 'p' + str(j+1))
        axes[i, 0].set_yticks(np.arange(0, np.max(data[i]) + 1))
        axes[i, 0].set_xlabel("time")
        axes[i, 0].set_ylabel("human count")
        axes[i, 0].legend(bbox_to_anchor=(1.05, 1.0), loc='upper left')
            
    fig.tight_layout()
    if(compareResultCount == 1):
        plt.savefig(savePath + '/Exhibition Realtime Human Count', dpi=600, bbox_inches='tight', pad_inches=0.02)
    else:
        plt.savefig(savePath + '/Compare Exhibition Realtime Human Count', dpi=600, bbox_inches='tight', pad_inches=0.02)

# social distance
def SocialDistance(filename):
    fig, axes = plt.subplots(1, 1, squeeze = False)
    fig.set_facecolor('xkcd:steel')
    
    #read data
    data = []
    # single
    if(compareResultCount == 1):
        fig.suptitle('Social Distance')
        file = path + folderList[0] + '/' + filename
        data.append(np.loadtxt(file, 'float'))
    # multiple
    else:
        fig.suptitle('Compare Social Distance')
        for i in range(compareResultCount):
            file = path + folderList[i] + '/' + filename;
            data.append(np.loadtxt(file, 'float'))
        
    # create dataframe list
    columnNames = ['dist', 'near', 'folderName']
    df = pd.DataFrame(columns = columnNames)
    for i in range(compareResultCount):
        for j in range(len(data[i])):
            if(data[i][j] <= 1.5):
                df = df.append({'dist' : data[i][j], 'near': 'Yes', 'folderName' : folderList[i]}, ignore_index = True )
            elif(data[i][j] <= 3):
                df = df.append({'dist' : data[i][j], 'near': 'No', 'folderName' : folderList[i]}, ignore_index = True )
        #df = df.append({'dist' : 1.2, 'near': 'No', 'folderName' : folderList[i]}, ignore_index = True )
    
    #plot
    sns.set_style('darkgrid')
    sns.violinplot(data = df, x = 'folderName', y = 'dist', hue = 'near', split = True, ax = axes[0, 0], 
                   inner = "quart", linewidth = 1, dodge = True, cut = 0)
    
    axes[0, 0].set_ylabel('distance')
    axes[0, 0].set_xlabel('')
    
    fig.tight_layout()
    if(compareResultCount == 1):
        plt.savefig(savePath + '/Social Distance', dpi=600, bbox_inches='tight', pad_inches=0.02)
    else:
        plt.savefig(savePath + '/Compare Social Distance', dpi=600, bbox_inches='tight', pad_inches=0.02)
    
# exhibition visiting time
def EachVisitorExhibitionVisitingTime(filename):
    # temp directory for save multiple pictures
    if(compareResultCount > 1):
        tmpDirectory = path + 'temp'
        os.makedirs(tmpDirectory)
    
    # read data
    dfList = []
    for i in range(compareResultCount):
        file = path + folderList[i] + '/' + filename
        df = pd.read_csv(file, sep = ' ', header = None)
        columnNames = []
        [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns) - 2)]
        columnNames.append('id')
        columnNames.append('humantype')
        df.columns = columnNames
        df.drop('id', inplace=True, axis=1)
        dfList.append(df)
    
    # plot
    for i in range(compareResultCount):
        fig, axes = plt.subplots(1, 1)
        plt.title(folderList[i] + ' each visitor\'s visiting time')
        pd.plotting.parallel_coordinates(dfList[i], 'humantype', ax = axes)
        a = dfList[i][columnNames[:-2]].max()
        b = max(a)
        c = round(b / 5)
        d = 5 * (c + 1)
        axes.set_yticks(np.arange(0, d, 5))
        #axes.legend_.remove()
        
        if(compareResultCount == 1):
            plt.savefig(savePath + '/Visiting Time', dpi=600, bbox_inches='tight', pad_inches=0.02)
        else:
            plt.savefig(tmpDirectory + '/' + filename.replace('.txt', '') + str(i), dpi=600, bbox_inches='tight', pad_inches=0.02)
    
    # combine image for multiple result
    imageList = []
    sizeWidth = []
    sizeHeight = []
    if(compareResultCount > 1):
        for i in range(compareResultCount):
            img = Image.open(tmpDirectory + '/' + filename.replace('.txt', '') + str(i) + '.png')
            imageList.append(img)
            sizeWidth.append(img.size[0])
            sizeHeight.append(img.size[1])
            
        imageSize = [max(sizeWidth), max(sizeHeight)]
        newImage = Image.new('RGB',(imageSize[0], compareResultCount * imageSize[1]), (250,250,250))
        
        for i in range(compareResultCount):
            newImage.paste(imageList[i], (0, i * imageSize[1]))
        newImage.save(savePath + '/Compare Visiting Time.png', 'png')
        #newImage.show()

        for root, dirs, files in os.walk(tmpDirectory, topdown=False):
            for name in files:
                os.remove(os.path.join(root, name))
        
        os.rmdir(tmpDirectory)

# exhibition transform opportunity
def ExhibitionTransformOpportunity(filename):
    # temp directory for save multiple pictures
    if(compareResultCount > 1):
        tmpDirectory = path + 'temp'
        os.makedirs(tmpDirectory)
    
    # read data
    data = []
    for i in range(compareResultCount):
        file = path + folderList[i] + '/' + filename
        data.append(np.loadtxt(file, 'float'))
    
    #grads = (True, False, False, True)                # gradient
    #gaps  = (0.03, 0, 0.03, 0)                        # gap value
    #sorts = ("size", "size", "distance", "distance")  # sort type
    #cclrs = (None, None, "slategrey", None)           # chord colors
    #nrota = (False, False, True, True)                # name rotation
    #cmaps = (None, None, None, "summer")              # colormap
    #fclrs = "grey"                                    # fontcolors

    # plot
    for i in range(compareResultCount):
        #fig, axes = plt.subplots(1, 1, figsize = (15, 15))
        # modify 0 value
        print(data[i].shape[0])
        
        for j in range(data[i].shape[0]):
            for k in range(data[i].shape[0]):
                if(j != k):
                    #print(data[i][j][k])
                    if( data[i][j, k] + data[i][k, j] != 0 ):
                        #print("j: " + str(j) + " k: " + str(k))
                        if(data[i][j, k] == 0): data[i][j, k] = 0.3
                        if(data[i][k, j] == 0): data[i][k, j] = 0.3
        
        
        print(data[i])
        names = []
        for j in range(data[i].shape[0] - 2):
            names.append('p' + str(j + 1))
        names.append('exit1')
        names.append('exit2')
        
        chord_diagram(data[i], names, gap = 0.03, use_gradient = False, sort = 'distance',
              cmap = None, chord_colors = None, rotate_names = False, fontcolor = 'grey')
        plt.title(folderList[i] + ' exhibition transform opportunity', y = 1.05)
        plt.tight_layout()
        if(compareResultCount == 1):
            plt.savefig(savePath + '/Exhibition Transform Opportunity', dpi=600, bbox_inches='tight', pad_inches=0.02)
        else:
            plt.savefig(tmpDirectory + '/' + filename.replace('.txt', '') + str(i), dpi=600, bbox_inches='tight', pad_inches=0.02)
            
    # combine image for multiple result
    imageList = []
    if(compareResultCount > 1):
        for i in range(compareResultCount):
            imageList.append(Image.open(tmpDirectory + '/' + filename.replace('.txt', '') + str(i) + '.png'))
            
        imageSize = imageList[0].size
        newImage = Image.new('RGB',(compareResultCount * imageSize[0], imageSize[1]), (250,250,250))
        
        for i in range(compareResultCount):
            newImage.paste(imageList[i], (i * imageSize[0], 0))
        newImage.save(savePath + '/Compare Exhibition Transform Opportunity.png', 'png')
        #newImage.show()
    
        for root, dirs, files in os.walk(tmpDirectory, topdown=False):
            for name in files:
                os.remove(os.path.join(root, name))
        os.rmdir(tmpDirectory)


# visitor status time
def VisitorStatusTime(filename):
    fig, axes = plt.subplots(1, compareResultCount, squeeze = False)
    fig.set_facecolor('xkcd:steel')
    
    #read data
    data = []
    # single
    if(compareResultCount == 1):
        fig.suptitle('visitor status time')
        file = path + folderList[0] + '/' + filename
        data.append(np.loadtxt(file, 'float'))
    # multiple
    else:
        fig.suptitle('Compare visitor status time')
        for i in range(compareResultCount):
            file = path + folderList[i] + '/' + filename;
            data.append(np.loadtxt(file, 'float'))
    
    #plot
    # plot
    for i in range(compareResultCount):
        arr = np.array(data[i])
        print(arr.shape)
        averageData = np.mean(arr, axis = 0)
        print(averageData)
        axes[0, i].set_title(folderList[i])
        axes[0, i].pie(averageData, labels = ['go', 'close', 'at'], autopct = "%1.1f%%", pctdistance = 0.6, textprops = {"fontsize" : 12}, shadow=True)
    
    fig.tight_layout()
    plt.show()
    '''
    if(compareResultCount == 1):
        plt.savefig(savePath + '/' + filename.replace('.txt', ''), dpi=600, bbox_inches='tight', pad_inches=0.02)
    else:
        plt.savefig(savePath + '/Compare ' + filename.replace('.txt', ''), dpi=600, bbox_inches='tight', pad_inches=0.02)
    '''

# visitor status time
def StatusTime(filename):
    #read data
    data = []
    # single
    if(compareResultCount == 1):
        file = path + folderList[0] + '/' + filename
        data.append(np.loadtxt(file, 'float'))
    # multiple
    else:
        for i in range(compareResultCount):
            file = path + folderList[i] + '/' + filename;
            data.append(np.loadtxt(file, 'float'))
    
    # plot
    for i in range(compareResultCount):
        arr = np.array(data[i])
        print(arr.shape)
        averageData = np.mean(arr, axis = 0)
        averageData = averageData / sum(averageData) 
        averageData = averageData * 100
        columnNames = ['folderName', 'go', 'close', 'at']
        df = pd.DataFrame(columns = columnNames)
        df = df.append({'folderName' : folderList[i], 'go': averageData[0], 'close' : averageData[1], 'at' : averageData[2]}, ignore_index = True )
        print(df)
        df.set_index('folderName').plot(kind='bar', stacked=True, color=['green', 'orange', 'red'])
        
        plt.title('visitor status time', fontsize=16)

        #add axis titles
        plt.xlabel('Folder Name')
        plt.ylabel('Percentage')
        
        #rotate x-axis labels
        plt.xticks(rotation=0)
    

# visitor status time
def StatusTimeBoxPlot(filename):
    fig, axes = plt.subplots(1, compareResultCount, squeeze = False)
    fig.set_facecolor('xkcd:steel')
    
    #read data
    data = []
    # single
    if(compareResultCount == 1):
        fig.suptitle('Status Time Box Plot')
        file = path + folderList[0] + '/' + filename
        data.append(np.loadtxt(file, 'float'))
    # multiple
    else:
        fig.suptitle('Compare Status Time Box Plot')
        for i in range(compareResultCount):
            file = path + folderList[i] + '/' + filename;
            data.append(np.loadtxt(file, 'float'))
    
    # plot
    for i in range(compareResultCount):
        columnNames = ['status', 'time']
        df = pd.DataFrame(columns = columnNames)
        
        for j in range(data[i].shape[0]):
            df = df.append({'status' : 'go', 'time': data[i][j][0]}, ignore_index = True)
            df = df.append({'status' : 'close', 'time': data[i][j][1]}, ignore_index = True)
            df = df.append({'status' : 'at', 'time': data[i][j][2]}, ignore_index = True)
        
        print(df)
        axes[0, i] = sns.boxplot(x="status", y="time", data=df)
        axes[0, i] = sns.swarmplot(x="status", y="time", data=df, color=".25")
        
    fig.tight_layout()
    plt.savefig(savePath + '/statusTime', dpi=600, bbox_inches='tight', pad_inches=0.02)

#EachVisitorExhibitionVisitingTimeBoxPlot
def EachVisitorExhibitionVisitingTimeBoxPlot(filename):
    # read data
    dfList = []
    for i in range(compareResultCount):
        file = path + folderList[i] + '/' + filename
        df = pd.read_csv(file, sep = ' ', header = None)
        columnNames = []
        [columnNames.append('p' + str(j + 1)) for j in range(len(df.columns) - 2)]
        columnNames.append('id')
        columnNames.append('humantype')
        df.columns = columnNames
        
        #handle data
        colNames = ['exhibition', 'time', 'id', 'humantype']
        new_df = pd.DataFrame(columns = colNames)
        print(len(df.index))
        for j in range(len(df.index)):
            for k in range(len(df.columns) - 2):
                exhibitionName = 'p' + str(k+1)
                new_df = new_df.append({'exhibition': exhibitionName, 'time': df.loc[j, exhibitionName], 
                                        'id': df.loc[j, 'id'], 'humantype': df.loc[j, 'humantype']}, ignore_index = True)
        
        #print(new_df)
        
        dfList.append(new_df)
    

    
    #plot
    for i in range(compareResultCount):
        ax = sns.boxplot(x="exhibition", y="time", hue = "humantype", data = dfList[i], palette="Set3")
        






















