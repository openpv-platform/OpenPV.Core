#ifndef VIEW_H
#define VIEW_H

#include <QMainWindow>

QT_BEGIN_NAMESPACE
namespace Ui { class View; }
QT_END_NAMESPACE

class View : public QMainWindow
{
    Q_OBJECT

public:
    View(QWidget *parent = nullptr);
    ~View();

private:
    Ui::View *ui;
    void Demo_Btn_Pressed();
    void Display_Btn_Pressed();
    void Electrical_Btn_Pressed();
    void Hardware_Btn_Pressed();
    void Interfaces_Btn_Pressed();
    void MoreInfo_Btn_Pressed();
    void Software_Btn_Pressed();
    void Home_Btn_Pressed();
};
#endif // VIEW_H
