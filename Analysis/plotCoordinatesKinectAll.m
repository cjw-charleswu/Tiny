function [] = plotCoordinatesKinectAll(coordinates_average_scenario_table)

joints_util;
plot_colors;

scenario_types = {
  'Stationary', 'Steps', 'Walk', 'Average'
};

rows = size(coordinates_average_scenario_table,1);

scenario_x = 1:length(scenario_types);
scenario_avg_dx = zeros(rows,1);
scenario_std_dx = zeros(rows,1);
scenario_avg_dy = zeros(rows,1);
scenario_std_dy = zeros(rows,1);
scenario_avg_dz = zeros(rows,1);
scenario_std_dz = zeros(rows,1);
scenario_avg_dd = zeros(rows,1);
scenario_std_dd = zeros(rows,1);

row_counter = 1;
for r = 1:size(coordinates_average_scenario_table,1);
    scen_table = coordinates_average_scenario_table(r,:);
    
    scenario_avg_dx(row_counter,1) = scen_table{1,'Joints_avg_dx'};
    scenario_std_dx(row_counter,1) = scen_table{1,'Joints_std_dx'};
    scenario_avg_dy(row_counter,1) = scen_table{1,'Joints_avg_dy'};
    scenario_std_dy(row_counter,1) = scen_table{1,'Joints_std_dy'};
    scenario_avg_dz(row_counter,1) = scen_table{1,'Joints_avg_dz'};
    scenario_std_dz(row_counter,1) = scen_table{1,'Joints_std_dz'};
    scenario_avg_dd(row_counter,1) = scen_table{1,'Joints_avg_dd'};
    scenario_std_dd(row_counter,1) = scen_table{1,'Joints_std_dd'};
    
    row_counter = row_counter+1;
end

figure;
hold on;
errorbar(scenario_x,scenario_avg_dx,scenario_std_dx,'MarkerEdgeColor',red,'MarkerFaceColor',red,'Color',red,'LineStyle','none','Marker','o');
errorbar(scenario_x,scenario_avg_dy,scenario_std_dy,'MarkerEdgeColor',green,'MarkerFaceColor',green,'Color',green,'LineStyle','none','Marker','o');
errorbar(scenario_x,scenario_avg_dz,scenario_std_dz,'MarkerEdgeColor',blue,'MarkerFaceColor',blue,'Color',blue,'LineStyle','none','Marker','o');
errorbar(scenario_x,scenario_avg_dd,scenario_std_dd,'MarkerEdgeColor',black,'MarkerFaceColor',black,'Color',black,'LineStyle','none','Marker','x','MarkerSize',10);
box on;
hold off;

title_format = 'Coordinates Averages over Parallel, 45%c and 90%c apart Kinects \n in the Stationary, Steps, and Walk Tasks';
dir = '../../KinectMultiTrackPlots/Overall/';
main_filename = strcat(dir,'Coordinates_Kinect_All');

plot_title = sprintf(title_format,char(176),char(176));
title(plot_title);
xlabel({'Scenarios'});
ylabel({'Distance (cm)'});
set(gca,'XLim',[0.5 length(scenario_types)+0.5]);
set(gca,'XTick',1:length(scenario_types),'XTickLabel',scenario_types);
ax = gca;
ax.XTickLabelRotation = -90;
legend('\Delta x','\Delta y','\Delta z','\Delta d','Location','northwest');

set(gcf,'Visible','Off');
savepdf(main_filename);

end
