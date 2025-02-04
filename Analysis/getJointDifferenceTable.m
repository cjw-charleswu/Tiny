function [difference_table, joints_difference_types] = getJointDifferenceTable(data_table)
%
% Study_Id Kinect_Config Scenario_Id Tracker_Time Person_Id ...
% Joint_1_dx Joint_1_dy Joint_1_dz Joint_1_dd ... Joint_N_dx ...
% 

joints_util;

first_variable_names = {
    'Study_Id','Kinect_Config','Scenario_Id','Tracker_Time','Person_Id'
};
first_joint_idx = length(first_variable_names)+1;

% 
% AnkleLeft_dx, AnkleLeft_dy, AnkleLeft_dz, AnkleLeft_dd, ...
% 
difference_types = {
    'dx','dy','dz','dd'
};
joints_difference_types = cell(1,length(joint_types)*length(difference_types));
counter = 0;
for jt = joint_types
    for d = difference_types
        counter = counter+1;
        joints_difference_types(1,counter) = strcat(jt,'_',d);
    end
end

table_variable_names = [first_variable_names joints_difference_types];
row_count = size(data_table,1)/2;
col_count = length(table_variable_names);
difference_table = array2table(zeros(row_count,col_count),'VariableNames',table_variable_names);
difference_row = struct();
for field = table_variable_names
    difference_row.(char(field)) = 0;
end

row_counter = 1;
filtered_variable_names = [first_variable_names coordinate_joint_types];
for s_id = unique(data_table.Study_Id,'rows').'
    s_table = data_table(data_table.Study_Id==s_id,filtered_variable_names);
    
    for k = unique(s_table.Kinect_Config,'rows').'
        k_table = s_table(s_table.Kinect_Config==k,:);
        
        for scen_id = unique(k_table.Scenario_Id,'rows').'
            scen_table = k_table(k_table.Scenario_Id==scen_id,:);
            
            fprintf('Calculating joint differences - Participant=%d, Kinect_Config=%d, Scenario=%d\n', ...
                s_id, k, scen_id);
            
            for t = unique(scen_table.Tracker_Time,'rows').'
                t_table = scen_table(scen_table.Tracker_Time==t,:);
                
                for p_id = unique(t_table.Person_Id,'rows').'
                    joints_data = t_table(t_table.Person_Id==p_id,:);

                    difference_row.Study_Id = s_id;
                    difference_row.Kinect_Config = k;
                    difference_row.Scenario_Id = scen_id;
                    difference_row.Tracker_Time = t;
                    difference_row.Person_Id = p_id;
                    
                    for jt_num = 1:length(joint_types)
                        % 3 because Joint_X, Joint_Y, Joint_Z
                        jt_idx = first_joint_idx + (jt_num-1)*3;
                        x=jt_idx; y=jt_idx+1; z=jt_idx+2;

                        % Assume 2 skeletons             
                        dx = abs(joints_data{1,x}-joints_data{2,x});
                        
                        display(joints_data{1,x});
                        display(joints_data{2,x});
                        display(dx);
                        display(a);
                        
                        dy = abs(joints_data{1,y}-joints_data{2,y});
                        dz = abs(joints_data{1,z}-joints_data{2,z});
                        dd = sqrt(dx.^2 + dy.^2 + dz.^2);

                        % 4 because Joint_dx, Joint_dy, Joint_dz, Joint_dd
                        jt_diff_type_idx = 1 + (jt_num-1)*4;

                        difference_row.(joints_difference_types{1,jt_diff_type_idx}) = dx;
                        difference_row.(joints_difference_types{1,jt_diff_type_idx+1}) = dy;
                        difference_row.(joints_difference_types{1,jt_diff_type_idx+2}) = dz;
                        difference_row.(joints_difference_types{1,jt_diff_type_idx+3}) = dd;
                    end

                    difference_table(row_counter,:) = struct2table(difference_row);
                    row_counter = row_counter+1;

                end
            end
        end
    end
end