# -*- coding: utf-8 -*-
"""
Spyder Editor

This is a temporary script file.
"""
import os
import method as md

md.path = '../Assets/StreamingAssets/Simulation_Result/'


if __name__ == '__main__':
    
    mode = input('mode: (s/m) ')
    if(mode == 's'):
        md.compareResultCount = 1
        folderName = input('Please input folder name: ')
        md.folderList.append(folderName)
        md.savePath = md.path + md.folderList[0] + '/Data Visualization'
        if(os.path.isdir(md.savePath) == False):
            os.makedirs(md.savePath)
            
    elif(mode == 'm'):
        md.compareResultCount = int(input('compare count: '))
        md.savePath = md.path + 'Compare Data Visualization'
        if(os.path.isdir(md.savePath) == False):
            os.makedirs(md.savePath)
        for i in range(md.compareResultCount):
            folderName = input('No.' + str(i) + ' folder name: ')
            md.folderList.append(folderName)
    
    
    md.Usage('space_usage.txt', 'int')
    '''
    md.Usage('time_usage.txt', 'float')
    '''
    #md.ExhibitionRealtimeHumanCount('ex_realtime_human_count.txt')
    
    
    #md.SocialDistance('social_distance.txt')
    #md.EachVisitorExhibitionVisitingTime('visiting_time.txt')
    #md.ExhibitionTransformOpportunity('ex_trans.txt')
    
    '''
    md.VisitorStatusime('status_time.txt')
    md.EachVisitorExhibitionVisitingTime('visiting_time.txt')
    
    
    md.StatusTime('status_time.txt')
    md.StatusTimeBoxPlot('status_time.txt')
    md.EachVisitorExhibitionVisitingTimeBoxPlot('visiting_time.txt')
    '''