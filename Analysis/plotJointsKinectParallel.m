function [] = plotJointsKinectParallel(joints_average_scenario_table)
% 
% joint types over joints averages for parallel kinects over the first 3
% tasks
% showing dx, dy, dz, dd for all joints
% 

joints_util;
plot_colors;

% average
k = 1;

joint_types_x = (1:length(joint_types))';

first_avg_dx = 3;
first_avg_dy = 5;
first_avg_dz = 7;
first_avg_dd = 9;
last_idx = 2+length(joint_types)*8;

% Assume one row (one person)
avg_dx = joints_average_scenario_table{k,first_avg_dx:8:last_idx}.';
std_dx = joints_average_scenario_table{k,first_avg_dx+1:8:last_idx}.';
avg_dy = joints_average_scenario_table{k,first_avg_dy:8:last_idx}.';
std_dy = joints_average_scenario_table{k,first_avg_dy+1:8:last_idx}.';
avg_dz = joints_average_scenario_table{k,first_avg_dz:8:last_idx}.';
std_dz = joints_average_scenario_table{k,first_avg_dz+1:8:last_idx}.';
avg_dd = joints_average_scenario_table{k,first_avg_dd:8:last_idx}.';
std_dd = joints_average_scenario_table{k,first_avg_dd+1:8:last_idx}.';

figure;
hold on;
errorbar(joint_types_x,avg_dx,std_dx,'MarkerEdgeColor',red,'MarkerFaceColor',red,'Color',red,'LineStyle','none','Marker','o');
errorbar(joint_types_x,avg_dy,std_dy,'MarkerEdgeColor',green,'MarkerFaceColor',green,'Color',green,'LineStyle','none','Marker','o');
errorbar(joint_types_x,avg_dz,std_dz,'MarkerEdgeColor',blue,'MarkerFaceColor',blue,'Color',blue,'LineStyle','none','Marker','o');
errorbar(joint_types_x,avg_dd,std_dd,'MarkerEdgeColor',black,'MarkerFaceColor',black,'Color',black,'LineStyle','none','Marker','x','MarkerSize',10);
box on;
hold off;

title_format = 'Joints Averages with Parallel Kinects over \n Stationary, Steps, and Walk Tasks';
dir = '../../KinectMultiTrackPlots/Overall/';
filename_format = strcat(dir,'Joints_Kinect_Parallel');
plot_title = sprintf(title_format);
plot_filename = sprintf(filename_format);

title(plot_title);
xlabel({'Joint Types'});
ylabel({'Distance (cm)'});
set(gca,'XLim',[0.5 length(joint_types)+0.5]);
set(gca,'XTick',1:length(joint_types),'XTickLabel',joint_types);
ax = gca;
ax.XTickLabelRotation = -90;
legend('\Delta x','\Delta y','\Delta z','\Delta d','Location','northwest');

set(gcf,'Visible','Off');
savepdf(plot_filename);

end